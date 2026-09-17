using System;

namespace NullProtocol.Core
{
    /// <summary>
    /// Global interaction state query to avoid input overlap between context interaction (F key)
    /// and alternative gadget deployment hotkeys.
    /// </summary>
    public static class InteractionState
    {
        public static Func<bool> IsNearOrUsingTerminal { get; set; }

        public static bool IsTerminalActive()
        {
            return IsNearOrUsingTerminal != null && IsNearOrUsingTerminal.Invoke();
        }
    }
}
