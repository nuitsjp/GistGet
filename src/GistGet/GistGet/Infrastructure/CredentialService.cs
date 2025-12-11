using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Diagnostics.CodeAnalysis;

namespace GistGet.Infrastructure;

public class CredentialService : ICredentialService
{
    public bool SaveCredential(string target, Credential credential)
    {
        var credStruct = new CREDENTIAL
        {
            Type = CRED_TYPE.GENERIC,
            TargetName = target,
            UserName = credential.Username,
            CredentialBlobSize = Encoding.UTF8.GetByteCount(credential.Token),
            CredentialBlob = Marshal.StringToCoTaskMemUTF8(credential.Token),
            Persist = CRED_PERSIST.LOCAL_MACHINE
        };

        try
        {
            if (!NativeMethods.CredWrite(ref credStruct, 0))
            {
                // var error = Marshal.GetLastWin32Error();
                // Log error?
                return false;
            }
            return true;
        }
        finally
        {
            Marshal.FreeCoTaskMem(credStruct.CredentialBlob);
        }
    }

    public bool TryGetCredential(string target, [NotNullWhen(true)] out Credential? credential)
    {
        credential = null;
        if (NativeMethods.CredRead(target, CRED_TYPE.GENERIC, 0, out var credentialPtr))
        {
            try
            {
                var credStruct = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                var username = credStruct.UserName;
                if (credStruct.CredentialBlob != IntPtr.Zero && credStruct.CredentialBlobSize > 0)
                {
                    var bytes = new byte[credStruct.CredentialBlobSize];
                    Marshal.Copy(credStruct.CredentialBlob, bytes, 0, credStruct.CredentialBlobSize);
                    var token = Encoding.UTF8.GetString(bytes);
                    credential = new Credential(username, token);
                    return true;
                }
            }
            finally
            {
                NativeMethods.CredFree(credentialPtr);
            }
        }
        return false;
    }

    public bool DeleteCredential(string target)
    {
        if (!NativeMethods.CredDelete(target, CRED_TYPE.GENERIC, 0))
        {
            var error = Marshal.GetLastWin32Error();
            // 1168 = ERROR_NOT_FOUND
            if (error != 1168)
            {
                return false;
            }
        }
        return true;
    }

    private static class NativeMethods
    {
        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredWrite(ref CREDENTIAL userCredential, uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredRead(string target, CRED_TYPE type, uint reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredDelete(string target, CRED_TYPE type, uint flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern void CredFree(IntPtr buffer);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    // ReSharper disable once InconsistentNaming
    private struct CREDENTIAL
    {
        public uint Flags;
        public CRED_TYPE Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CRED_PERSIST Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    // ReSharper disable once InconsistentNaming
    public enum CRED_TYPE : uint
    {
        // ReSharper disable once InconsistentNaming
        GENERIC = 1
    }

    // ReSharper disable once InconsistentNaming
    public enum CRED_PERSIST : uint
    {
        // ReSharper disable UnusedMember.Global
        // ReSharper disable InconsistentNaming
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3
        // ReSharper restore InconsistentNaming
        // ReSharper restore UnusedMember.Global
    }
}
