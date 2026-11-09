using Application.ClientWindow;
using Application.ClientWindow.UIHandlers;
using Xunit;

namespace Application.Tests;

public class KeyboardModifierGuardTests
{
    public static IEnumerable<object[]> ModifierKeys => new[]
    {
        new object[] { WinApi.VirtualKeyShort.VK_CONTROL },
        new object[] { WinApi.VirtualKeyShort.VK_MENU },
        new object[] { WinApi.VirtualKeyShort.VK_SHIFT },
    };

    [Theory]
    [MemberData(nameof(ModifierKeys))]
    public void TryPressButton_DoesNotSendWhenModifierIsHeld(WinApi.VirtualKeyShort heldModifier)
    {
        var sentButtons = new List<int>();
        var keyboard = new Keyboard(
            IntPtr.Zero,
            key => key == (int)heldModifier ? unchecked((short)0x8000) : (short)0,
            sentButtons.Add);

        var sent = keyboard.TryPressButton((int)WinApi.VirtualKeyShort.VK_F1);

        Assert.False(sent);
        Assert.Empty(sentButtons);
    }

    [Fact]
    public void TryPressButton_SendsOnceWhenNoModifierIsHeld()
    {
        var sentButtons = new List<int>();
        var keyboard = new Keyboard(IntPtr.Zero, _ => 0, sentButtons.Add);
        const int moduleKey = (int)WinApi.VirtualKeyShort.VK_F1;

        var sent = keyboard.TryPressButton(moduleKey);

        Assert.True(sent);
        Assert.Equal(new[] { moduleKey }, sentButtons);
    }

    [Fact]
    public void IsAnyModifierKeyDown_IgnoresTheReleasedBit()
    {
        var keyboard = new Keyboard(IntPtr.Zero, _ => 1);

        Assert.False(keyboard.IsAnyModifierKeyDown());
    }
}
