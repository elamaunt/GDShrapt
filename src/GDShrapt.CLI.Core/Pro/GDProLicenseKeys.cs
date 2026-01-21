namespace GDShrapt.CLI.Core;

/// <summary>
/// Public key for license verification.
/// Same key is used in Pro.Core for actual verification.
/// This allows CLI.Core to verify basic license format if needed.
/// </summary>
internal static class GDProLicenseKeys
{
    /// <summary>
    /// RSA 2048-bit public key in DER format (Base64).
    /// Must match the key in GDShrapt.Pro.Core.GDProLicenseKeys.
    /// </summary>
    internal const string PublicKeyBase64 =
        "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA2hgCQnprMeb/UMBa67Fu" +
        "Fm3Gyykrm/rNo9jdeLBVSAwrOMNOek3aqrjHnzvV+WfcQUYjT5oBn3NldqZY9kMr" +
        "rGv5822e5pYCgZqUikX46QsCX54DP9S6+mWb5JeBKEE3dZuDAFBqcbEsElHCU+5Z" +
        "U3oS7HfyZLiIn1dVZhyg1jMZZfUOFo65eQBIapwX17MmjZxd+qRF+lpzATuQUPy1" +
        "VhrjC8lh+oK1efUdXQ/w60nUFWA7LNgMHFS/DAZ5tWF5OodOWFm8vMw+5uMtPbLT" +
        "x0tBkdHCSO39DGKnBnZGwGMTASCA2VB4Yk5he/XnE8408SE15Jbh8A9QAwxg2KBw" +
        "hQIDAQAB";
}
