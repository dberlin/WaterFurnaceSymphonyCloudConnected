// Copyright (c) Daniel Berlin. All rights reserved.
// </copyright>

namespace WaterFurnaceCommon
{
    using System.Runtime.CompilerServices;
    using Crestron.SimplSharp;

    /// <summary>
    ///     Basic logging class that prints messages to the crestron console.
    /// </summary>
    public static class WaterFurnaceLogging
    {
        public static void TraceMessage(
            bool enabled,
            string message = "",
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!enabled) return;
            CrestronConsole.Print("message: ");
            foreach (var str in message.Split('\n'))
                CrestronConsole.PrintLine(str.TrimEnd('\r'));
            CrestronConsole.PrintLine("member name: " + memberName);
            CrestronConsole.PrintLine("source file path: " + sourceFilePath);
            CrestronConsole.PrintLine("source line number: " + sourceLineNumber);
        }
    }
}