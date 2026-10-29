using System;
using System.Runtime.InteropServices;

namespace MCERemote.Receiver.Helpers
{
    [ComImport]
    [Guid("00000016-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleMessageFilter
    {
        [PreserveSig]
        int HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo);

        [PreserveSig]
        int RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType);

        [PreserveSig]
        int MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType);
    }

    /// <summary>
    /// COM message filter that retries calls rejected by a busy out-of-process server
    /// (RPC_E_CALL_REJECTED / RPC_E_SERVERCALL_RETRYLATER), which the remoted WMP
    /// instance does while it is busy, e.g. during track changes.
    /// Only affects COM calls made from the STA thread it is registered on.
    /// </summary>
    internal class OleMessageFilter : IOleMessageFilter
    {
        private const int SERVERCALL_ISHANDLED = 0;
        private const int SERVERCALL_RETRYLATER = 2;
        private const int PENDINGMSG_WAITDEFPROCESS = 2;

        /// <summary>
        /// How long to keep retrying a rejected call before giving up, in milliseconds.
        /// </summary>
        private const int RetryTimeout = 10000;

        /// <summary>
        /// Delay between retries, in milliseconds.
        /// </summary>
        private const int RetryDelay = 100;

        private static OleMessageFilter current;

        [DllImport("ole32.dll")]
        private static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldFilter);

        /// <summary>
        /// Registers the filter on the calling thread, which must be an STA thread.
        /// </summary>
        public static void Register()
        {
            current = new OleMessageFilter();
            CoRegisterMessageFilter(current, out _);
        }

        /// <summary>
        /// Removes the filter from the calling thread.
        /// </summary>
        public static void Revoke()
        {
            CoRegisterMessageFilter(null, out _);
            current = null;
        }

        int IOleMessageFilter.HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo)
        {
            return SERVERCALL_ISHANDLED;
        }

        int IOleMessageFilter.RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType)
        {
            if (dwRejectType == SERVERCALL_RETRYLATER && dwTickCount < RetryTimeout)
                return RetryDelay;

            // Cancel the call, the caller gets the original exception
            return -1;
        }

        int IOleMessageFilter.MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType)
        {
            return PENDINGMSG_WAITDEFPROCESS;
        }
    }
}
