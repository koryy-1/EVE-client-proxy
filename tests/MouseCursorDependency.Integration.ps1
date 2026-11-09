param(
    [string]$ApiBase = 'http://127.0.0.1:5020',
    [string]$TargetName = 'Caretyn - Star',
    [switch]$AllowPhysicalCursorMoves
)

$ErrorActionPreference = 'Stop'

if (-not $AllowPhysicalCursorMoves) {
    Write-Output 'SKIP: this integration test temporarily moves the desktop cursor. Re-run with -AllowPhysicalCursorMoves when the bot is stopped.'
    exit 0
}

Add-Type -AssemblyName System.Windows.Forms
if ($null -eq ('EveMouseIntegrationNative' -as [type])) {
    $formsAssembly = [System.Windows.Forms.Form].Assembly.Location
    $drawingAssembly = [System.Drawing.Rectangle].Assembly.Location
    Add-Type -ReferencedAssemblies @($formsAssembly, $drawingAssembly) -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
public static class EveMouseIntegrationNative {
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll", SetLastError=true)] public static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll", SetLastError=true)] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll", SetLastError=true)] public static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll", SetLastError=true)] public static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);
    [DllImport("user32.dll", SetLastError=true)] public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError=true)] public static extern bool PostMessage(IntPtr hwnd, uint msg, int wParam, int lParam);

    public static int MakeLParam(int x, int y) { return (y << 16) | (x & 0xffff); }
    public static IntPtr RootWindowFromPoint(POINT point) {
        return GetAncestor(WindowFromPoint(point), 2); // GA_ROOT
    }
}

public static class EveMouseTestOverlay {
    private sealed class PassiveOverlayForm : Form {
        public PassiveOverlayForm() {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.OrangeRed;
            Opacity = 0.85;
            Text = "Temporary non-activating mouse test overlay";
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000 | 0x00000080; // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
                return cp;
            }
        }
    }

    private static readonly ManualResetEvent Ready = new ManualResetEvent(false);
    private static Thread OverlayThread;
    private static PassiveOverlayForm OverlayForm;
    private static Exception OverlayError;

    public static IntPtr ShowAt(int x, int y, int width, int height) {
        Ready.Reset();
        OverlayError = null;
        OverlayThread = new Thread(delegate() {
            try {
                OverlayForm = new PassiveOverlayForm();
                OverlayForm.StartPosition = FormStartPosition.Manual;
                OverlayForm.Bounds = new Rectangle(x, y, width, height);
                OverlayForm.Shown += delegate { Ready.Set(); };
                Application.Run(OverlayForm);
            } catch (Exception exception) {
                OverlayError = exception;
                Ready.Set();
            }
        });
        OverlayThread.IsBackground = true;
        OverlayThread.SetApartmentState(ApartmentState.STA);
        OverlayThread.Start();
        if (!Ready.WaitOne(5000))
            throw new InvalidOperationException("The temporary overlay window did not become ready.");
        if (OverlayError != null)
            throw new InvalidOperationException("Could not create the temporary overlay window.", OverlayError);
        if (OverlayForm == null || !OverlayForm.IsHandleCreated)
            throw new InvalidOperationException("The temporary overlay has no window handle.");
        return OverlayForm.Handle;
    }

    public static void Close() {
        PassiveOverlayForm form = OverlayForm;
        if (form != null && !form.IsDisposed && form.IsHandleCreated) {
            try { form.BeginInvoke(new Action(delegate { form.Close(); })); } catch { }
        }
        if (OverlayThread != null && OverlayThread.IsAlive) OverlayThread.Join(2000);
        OverlayForm = null;
        OverlayThread = null;
    }
}
'@
}
$nativeType = 'EveMouseIntegrationNative' -as [type]
$overlayType = 'EveMouseTestOverlay' -as [type]
if ($null -eq $nativeType -or $null -eq $overlayType) {
    throw 'The diagnostic types are only partially loaded. Start a fresh PowerShell session and run the script again.'
}
if ($null -eq $nativeType.GetMethod('RootWindowFromPoint') -or $null -eq $overlayType.GetMethod('ShowAt')) {
    throw 'Older diagnostic types are loaded in this PowerShell session. Start a fresh PowerShell session and run the script again.'
}

function Get-SelectedName {
    $info = Invoke-RestMethod -Uri "$ApiBase/api/SelectItem/get-select-item-info" -TimeoutSec 5
    if ($null -eq $info.name) { return '' }
    return [string]$info.name
}

function Assert-NoOverviewLocks {
    $items = Invoke-RestMethod -Uri "$ApiBase/api/OverView/get-overview-info" -TimeoutSec 5
    $locked = @($items | Where-Object {
        $_.targetLocked -eq $true -or $_.aimOnTargetLocked -eq $true -or $_.lockInProgress -eq $true
    })
    if ($locked.Count -gt 0) {
        throw 'Safety check failed: a target is locked, aimed, or being locked; test aborted.'
    }
}

function Test-SelectedTarget([string]$Name) {
    $deadline = [DateTime]::UtcNow.AddSeconds(2)
    do {
        if ((Get-SelectedName).StartsWith($Name, [StringComparison]::OrdinalIgnoreCase)) { return $true }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)
    return $false
}

function Invoke-SelectionClick($Item) {
    Assert-NoOverviewLocks
    $json = $Item | ConvertTo-Json -Depth 10 -Compress
    # The API's success response only means the request was handled. The assertion
    # below reads actual Selected Item state instead of trusting HTTP 200.
    Invoke-RestMethod -Uri "$ApiBase/api/OverView/click-on-object" -Method Post `
        -Body $json -ContentType 'application/json' -TimeoutSec 5 | Out-Null
    return Test-SelectedTarget $Item.name
}

function Set-CursorOverOverviewItem($Item, [IntPtr]$Hwnd) {
    $point = New-Object EveMouseIntegrationNative+POINT
    $point.X = [int]$Item.pos.x
    $point.Y = [int]$Item.pos.y
    if (-not [EveMouseIntegrationNative]::ClientToScreen($Hwnd, [ref]$point)) {
        throw "Could not map Overview row '$($Item.name)' to screen coordinates."
    }
    return [EveMouseIntegrationNative]::SetCursorPos($point.X, $point.Y)
}

function Ensure-SelectedOverviewItem($Item, [IntPtr]$Hwnd) {
    $selected = Get-SelectedName
    if ($selected.StartsWith($Item.name, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    if (-not (Set-CursorOverOverviewItem $Item $Hwnd)) { return $false }
    return Invoke-SelectionClick $Item
}

$originalCursor = New-Object EveMouseIntegrationNative+POINT
if (-not [EveMouseIntegrationNative]::GetCursorPos([ref]$originalCursor)) {
    throw 'Could not read the desktop cursor position.'
}
$overlayHandle = [IntPtr]::Zero

try {
    $ws = Invoke-RestMethod -Uri "$ApiBase/api/BotConnection/check-bot-connection" -TimeoutSec 3
    if (-not $ws) { throw 'Bot WebSocket is disconnected.' }
    if (-not (Invoke-RestMethod -Uri "$ApiBase/api/GameClient/check-root-address-actuality" -TimeoutSec 3)) {
        throw 'The UI root address is not current.'
    }

    $client = Invoke-RestMethod -Uri "$ApiBase/api/GameClient/get-client-params" -TimeoutSec 3
    $hwnd = [IntPtr]([long]$client.hWnd)
    if ($hwnd -eq [IntPtr]::Zero) { throw 'EVE HWND is empty.' }
    $foregroundBefore = [EveMouseIntegrationNative]::GetForegroundWindow()
    if ($foregroundBefore -eq $hwnd) {
        Write-Output 'SKIP: EVE is currently foreground. Focus another window manually; this test does not change foreground activation.'
        exit 0
    }
    if (-not [EveMouseIntegrationNative]::IsWindowVisible($hwnd) -or [EveMouseIntegrationNative]::IsIconic($hwnd)) {
        Write-Output 'SKIP: EVE is hidden or minimized; background hit-testing cannot be compared.'
        exit 0
    }

    $overview = Invoke-RestMethod -Uri "$ApiBase/api/OverView/get-overview-info" -TimeoutSec 5
    $target = $overview | Where-Object { $_.name -eq $TargetName } | Select-Object -First 1
    if ($null -eq $target) { throw "Overview target '$TargetName' was not found." }
    Assert-NoOverviewLocks

    $initialName = Get-SelectedName
    if ($initialName.StartsWith($target.name, [StringComparison]::OrdinalIgnoreCase)) {
        $target = $overview | Where-Object { $_.name -ne $TargetName } | Select-Object -First 1
        if ($null -eq $target) { throw 'No alternative Overview row is available for a state-change check.' }
    }

    $clientRect = New-Object EveMouseIntegrationNative+RECT
    if (-not [EveMouseIntegrationNative]::GetClientRect($hwnd, [ref]$clientRect)) {
        throw 'GetClientRect failed.'
    }
    $clientTopLeft = New-Object EveMouseIntegrationNative+POINT
    $clientBottomRight = New-Object EveMouseIntegrationNative+POINT
    $clientBottomRight.X = $clientRect.Right - 1
    $clientBottomRight.Y = $clientRect.Bottom - 1
    if (-not [EveMouseIntegrationNative]::ClientToScreen($hwnd, [ref]$clientTopLeft) -or
        -not [EveMouseIntegrationNative]::ClientToScreen($hwnd, [ref]$clientBottomRight)) {
        throw 'ClientToScreen failed.'
    }

    $clientBounds = [System.Drawing.Rectangle]::new(
        $clientTopLeft.X,
        $clientTopLeft.Y,
        ($clientBottomRight.X - $clientTopLeft.X + 1),
        ($clientBottomRight.Y - $clientTopLeft.Y + 1))

    $outsidePoint = $null
    foreach ($screen in [Windows.Forms.Screen]::AllScreens) {
        $bounds = $screen.Bounds
        $candidates = @(
            @{ X = $bounds.Left + [int]($bounds.Width / 2); Y = $bounds.Top + [int]($bounds.Height / 2) },
            @{ X = $bounds.Left + 8; Y = $bounds.Top + 8 },
            @{ X = $bounds.Right - 9; Y = $bounds.Bottom - 9 },
            @{ X = $bounds.Left + 8; Y = $bounds.Bottom - 9 },
            @{ X = $bounds.Right - 9; Y = $bounds.Top + 8 }
        )
        foreach ($candidate in $candidates) {
            $point = [System.Drawing.Point]::new($candidate.X, $candidate.Y)
            if ($bounds.Contains($point) -and -not $clientBounds.Contains($point)) {
                $outsidePoint = $point
                break
            }
        }
        if ($null -ne $outsidePoint) { break }
    }

    if ($null -eq $outsidePoint) {
        $monitorCount = [Windows.Forms.Screen]::AllScreens.Count
        if ($monitorCount -eq 1) {
            Write-Output 'SKIP: one monitor and the EVE client covers all usable screen pixels (fullscreen); there is no physical cursor position outside the client area.'
            exit 0
        }
        Write-Output 'SKIP: no visible screen point outside the EVE client area was found.'
        exit 0
    }

    $targetClientPoint = New-Object EveMouseIntegrationNative+POINT
    $targetClientPoint.X = [int]$target.pos.x
    $targetClientPoint.Y = [int]$target.pos.y
    $targetScreenPoint = New-Object EveMouseIntegrationNative+POINT
    $targetScreenPoint.X = $targetClientPoint.X
    $targetScreenPoint.Y = $targetClientPoint.Y
    if (-not [EveMouseIntegrationNative]::ClientToScreen($hwnd, [ref]$targetScreenPoint)) {
        throw 'Could not map the target Overview point to screen coordinates.'
    }
    # This records whether Windows accepted the posted WM_MOUSEMOVE into the
    # target window queue in each cursor position. It does NOT prove that EVE
    # consumed the message; selection state below is the app-level observation.
    $outsideSet = [EveMouseIntegrationNative]::SetCursorPos($outsidePoint.X, $outsidePoint.Y)
    if (-not $outsideSet) { throw 'Could not place the cursor outside the EVE client for the test.' }
    $outsideCursorPoint = New-Object EveMouseIntegrationNative+POINT
    $outsideCursorPoint.X = $outsidePoint.X
    $outsideCursorPoint.Y = $outsidePoint.Y
    $topWindowAtOutside = [EveMouseIntegrationNative]::RootWindowFromPoint($outsideCursorPoint)
    $moveOutsideAccepted = [EveMouseIntegrationNative]::PostMessage(
        $hwnd, 0x0200, 0,
        [EveMouseIntegrationNative]::MakeLParam($targetClientPoint.X, $targetClientPoint.Y))
    $outsideSelected = Invoke-SelectionClick $target

    # Re-establish a known different selection so the next case measures an actual
    # state transition even if the outside-cursor click happened to work.
    $resetTarget = $overview | Where-Object {
        $initialName.StartsWith($_.name, [StringComparison]::OrdinalIgnoreCase) -and $_.name -ne $target.name
    } | Select-Object -First 1
    if ($null -eq $resetTarget) {
        $resetTarget = $overview | Where-Object { $_.name -ne $target.name } | Select-Object -First 1
    }
    if ($null -eq $resetTarget) { throw 'No second Overview row is available for the overlay comparison.' }
    if (-not (Ensure-SelectedOverviewItem $resetTarget $hwnd)) {
        throw 'Could not reset Selected Item before the visible-target comparison.'
    }

    $targetSet = [EveMouseIntegrationNative]::SetCursorPos($targetScreenPoint.X, $targetScreenPoint.Y)
    if (-not $targetSet) { throw 'Could not place the cursor over the target row.' }
    $topWindowAtTargetNow = [EveMouseIntegrationNative]::RootWindowFromPoint($targetScreenPoint)
    $moveOnTargetAccepted = [EveMouseIntegrationNative]::PostMessage(
        $hwnd, 0x0200, 0,
        [EveMouseIntegrationNative]::MakeLParam($targetClientPoint.X, $targetClientPoint.Y))
    $onTargetSelected = Invoke-SelectionClick $target

    # Reset once more, then put a small non-activating test window over the EVE
    # row. This emulates the terminal-over-EVE arrangement without changing the
    # foreground window or sending input to the overlay.
    if (-not (Ensure-SelectedOverviewItem $resetTarget $hwnd)) {
        throw 'Could not reset Selected Item before the overlay comparison.'
    }
    $overlayHandle = [EveMouseTestOverlay]::ShowAt(
        ($targetScreenPoint.X - 45), ($targetScreenPoint.Y - 12), 90, 24)
    Start-Sleep -Milliseconds 150
    if ([EveMouseIntegrationNative]::GetForegroundWindow() -ne $foregroundBefore) {
        throw 'Foreground changed while creating the overlay; the test will not continue.'
    }
    $overlayCursorSet = [EveMouseIntegrationNative]::SetCursorPos($targetScreenPoint.X, $targetScreenPoint.Y)
    if (-not $overlayCursorSet) { throw 'Could not place the cursor on the temporary overlay.' }
    $topWindowAtOverlayCursor = [EveMouseIntegrationNative]::RootWindowFromPoint($targetScreenPoint)
    $overlaySelected = Invoke-SelectionClick $target

    Write-Output "Target: $($target.name)"
    Write-Output "EVE foreground: no (foreground is left unchanged)"
    Write-Output "Top-level window under cursor outside client: $topWindowAtOutside"
    Write-Output "Top-level window under target cursor: $topWindowAtTargetNow (EVE HWND: $hwnd; target occluded: $($topWindowAtTargetNow -ne $hwnd))"
    Write-Output "PostMessage(WM_MOUSEMOVE) accepted outside client: $moveOutsideAccepted"
    Write-Output "PostMessage(WM_MOUSEMOVE) accepted over target: $moveOnTargetAccepted"
    Write-Output "Selection changed with cursor outside client: $outsideSelected"
    Write-Output "Selection changed with cursor over target: $onTargetSelected"
    Write-Output "Top-level window at target with test overlay: $topWindowAtOverlayCursor (overlay HWND: $overlayHandle)"
    Write-Output "Selection changed with cursor on overlay over EVE: $overlaySelected"

    if ($outsideSelected -and $onTargetSelected -and $overlaySelected) {
        Write-Output 'PASS: EVE selection succeeded outside the client, over the visible target, and with a non-activating overlay covering that target.'
        exit 0
    }
    if (-not $outsideSelected -and $onTargetSelected) {
        if (-not $overlaySelected) {
            Write-Output 'FAIL: selection required the cursor over the visible target; it did not work outside the client or with the overlay covering the target.'
        } else {
            Write-Output 'FAIL (cursor-position dependence observed): selection worked with the cursor at the target coordinates, including over the overlay, but not with the cursor outside EVE.'
        }
        exit 1
    }
    if ($outsideSelected -and $onTargetSelected -and -not $overlaySelected) {
        Write-Output 'FAIL: cursor position did not matter while EVE was visible, but the overlaid-window case did not register.'
        exit 1
    }
    if ($topWindowAtTargetNow -ne $hwnd) {
        Write-Output 'INCONCLUSIVE: the target point is covered by another top-level window and the target selection did not register. This run cannot distinguish cursor gating from occlusion gating.'
        exit 2
    }
    Write-Output 'INCONCLUSIVE: EVE did not select the row in both conditions; check target visibility, coordinates, UI refresh, and current client build.'
    exit 2
}
finally {
    [void][EveMouseTestOverlay]::Close()
    # The test never activates EVE and restores the user's original cursor.
    [void][EveMouseIntegrationNative]::SetCursorPos($originalCursor.X, $originalCursor.Y)
}
