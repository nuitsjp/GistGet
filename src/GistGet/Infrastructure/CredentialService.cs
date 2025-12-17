// Credential persistence using Windows Credential Manager.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

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
        ArgumentNullException.ThrowIfNull(credential);

        var credStruct = new NativeCredential
        {
            Type = CredType.Generic,
            TargetName = targetName,
            UserName = credential.Username,
            CredentialBlobSize = Encoding.UTF8.GetByteCount(credential.Token),
            CredentialBlob = Marshal.StringToCoTaskMemUTF8(credential.Token),
            Persist = CredPersist.LocalMachine
        };

        try
        {
            return WriteCredentialToStore(ref credStruct);
        }
        finally
        {
            Marshal.FreeCoTaskMem(credStruct.CredentialBlob);
        }
    }

    /// <summary>
    /// Attempts to read the stored credential.
    /// </summary>
    public bool TryGetCredential(out Credential credential)
    {
        credential = null!;
        if (NativeMethods.CredRead(targetName, CredType.Generic, 0, out var credentialPtr))
        {
            try
            {
                var credStruct = Marshal.PtrToStructure<NativeCredential>(credentialPtr);
                var username = credStruct.UserName;
                return TryExtractCredentialFromBlob(credStruct, username, out credential);
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
        if (!NativeMethods.CredDelete(targetName, CredType.Generic, 0))
        {
            return HandleDeleteError();
        }
        return true;
    }

    /// <summary>
    /// Writes credential to Windows Credential Manager.
    /// Defensive: handles rare API write failures.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static bool WriteCredentialToStore(ref NativeCredential credStruct)
    {
        if (!NativeMethods.CredWrite(ref credStruct, 0))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Extracts credential data from native blob.
    /// Defensive: handles corrupted or empty credential blobs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static bool TryExtractCredentialFromBlob(NativeCredential credStruct, string username, out Credential credential)
    {
        credential = null!;
        if (credStruct.CredentialBlob != IntPtr.Zero && credStruct.CredentialBlobSize > 0)
        {
            var bytes = new byte[credStruct.CredentialBlobSize];
            Marshal.Copy(credStruct.CredentialBlob, bytes, 0, credStruct.CredentialBlobSize);
            var token = Encoding.UTF8.GetString(bytes);
            credential = new Credential(username, token);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Handles errors from CredDelete API.
    /// Defensive: treats NOT_FOUND (1168) as success, other errors as failure.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static bool HandleDeleteError()
    {
        var error = Marshal.GetLastWin32Error();
        if (error != 1168) // ERROR_NOT_FOUND
        {
            return false;
        }
        return true;
    }

    private static class NativeMethods
    {
        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredWrite(ref NativeCredential userCredential, uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredRead(string target, CredType type, uint reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredDelete(string target, CredType type, uint flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern void CredFree(IntPtr buffer);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public CredType Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CredPersist Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    private enum CredType : uint
    {
        Generic = 1
    }

    // ReSharper disable UnusedMember.Local
#pragma warning disable IDE0051 // Remove unused private members
    private enum CredPersist : uint
    {
        Session = 1,
        LocalMachine = 2,
        Enterprise = 3
    }
#pragma warning restore IDE0051
    // ReSharper restore UnusedMember.Local
}
