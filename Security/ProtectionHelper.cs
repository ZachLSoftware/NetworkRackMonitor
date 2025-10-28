using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor.Security
{
    public static class ProtectionHelper
    {
        private static readonly byte[] s_additionalEntropy = { 0x1A, 0x2B, 0x3C, 0x4D, 0x5E };

        public static string ProtectString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            try
            {
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(
                    plainTextBytes,
                    s_additionalEntropy,
                    DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (CryptographicException ex)
            {
                Debug.WriteLine($"Error protecting string: {ex.Message}");
                return null;
            }
        }

        public static string UnprotectString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return string.Empty; 
            }

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

              
                byte[] plainTextBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    s_additionalEntropy, 
                    DataProtectionScope.CurrentUser); 

                
                return Encoding.UTF8.GetString(plainTextBytes);
            }
            catch (FormatException ex) 
            {
                Debug.WriteLine($"Error unprotecting string (Invalid Base64): {ex.Message}");
                return null;
            }
            catch (CryptographicException ex)
            {
                Debug.WriteLine($"Error unprotecting string: {ex.Message}");
                return null; 
            }
        }
    }
}
