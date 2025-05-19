using System;
using CredentialManagement;

namespace GameTranslationTool.Utils
{
    /// <summary>
    /// Stores and retrieves the Google Translate API key in Windows Credential Manager.
    /// </summary>
    public static class SecureVault
    {
        private const string TargetName = "GameTranslator.GoogleKey";
        private const string MicrosoftTarget = "GameTranslator.MicrosoftKey";


        /// <summary>
        /// Store or update the Google API key in Windows Credential Manager.
        /// </summary>
        public static void SaveGoogleKey(string apiKey)
        {
            using var cred = new Credential
            {
                Target = TargetName,
                Username = Environment.UserName,
                Password = apiKey,
                PersistanceType = PersistanceType.LocalComputer
            };
            cred.Save();
        }

        /// Load the Google API key, or return null if not found.
        public static string? LoadGoogleKey()
        {
            using var cred = new Credential { Target = TargetName };
            return cred.Load() ? cred.Password : null;
        }
        
        /// Delete the stored Google API key.
        public static void DeleteGoogleKey()
        {
            using var cred = new Credential { Target = TargetName };
            cred.Delete();
        }

        public static void SaveMicrosoftKey(string apiKey)
        {
            using var cred = new Credential
            {
                Target = MicrosoftTarget,
                Username = Environment.UserName,
                Password = apiKey,
                PersistanceType = PersistanceType.LocalComputer
            };
            cred.Save();
        }

        public static string? LoadMicrosoftKey()
        {
            using var cred = new Credential { Target = MicrosoftTarget };
            return cred.Load() ? cred.Password : null;
        }

        public static void DeleteMicrosoftKey()
        {
            using var cred = new Credential { Target = MicrosoftTarget };
            cred.Delete();
        }
    }
}