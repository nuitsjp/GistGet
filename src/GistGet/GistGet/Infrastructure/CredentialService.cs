// Credential persistence using Windows Credential Manager.

using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Diagnostics.CodeAnalysis;

namespace GistGet.Infrastructure;

/// <summary>
/// Stores and retrieves GitHub credentials using the Windows Credential Manager.
/// </summary>
public class CredentialService(string targetName) : ICredentialService
{
    public CredentialService() : this("gistget:https://github.com/nuitsjp/GistGet")
    {
    }

    /// <summary>
    /// Persists a credential.
    /// </summary>
    public bool SaveCredential(Credential credential)
    {
        var credStruct = new CREDENTIAL
        {
            Type = CRED_TYPE.GENERIC,
            TargetName = targetName,
            UserName = credential.Username,
            CredentialBlobSize = Encoding.UTF8.GetByteCount(credential.Token),
            CredentialBlob = Marshal.StringToCoTaskMemUTF8(credential.Token),
            Persist = CRED_PERSIST.LOCAL_MACHINE
        };

        try
        {
            if (!NativeMethods.CredWrite(ref credStruct, 0))
            {
                return false;
            }
            return true;
        }
        finally
        {
            Marshal.FreeCoTaskMem(credStruct.CredentialBlob);
        }
    }

    /// <summary>
    /// Attempts to read the stored credential.
    /// </summary>
    public bool TryGetCredential([NotNullWhen(true)] out Credential credential)
    {
        credential = null!;
        if (NativeMethods.CredRead(targetName, CRED_TYPE.GENERIC, 0, out var credentialPtr))
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

    /// <summary>
    /// Deletes any stored credential.
    /// </summary>
    public bool DeleteCredential()
    {
        if (!NativeMethods.CredDelete(targetName, CRED_TYPE.GENERIC, 0))
        {
            var error = Marshal.GetLastWin32Error();
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

    public enum CRED_TYPE : uint
    {
        GENERIC = 1
    }

    public enum CRED_PERSIST : uint
    {
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3
    }
}
