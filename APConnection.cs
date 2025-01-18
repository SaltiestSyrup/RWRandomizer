using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainWorldRandomizer
{
    public static class APConnection
    {
        private const string APVersion = "0.6.0";

        public static bool Authenticated = false;
        public static bool HasConnected = false;

        public static ArchipelagoSession Session;

        private static void CreateSession(string hostName, int port)
        {
            Session = ArchipelagoSessionFactory.CreateSession(hostName, port);
        }

        public static string Connect(string hostName, int port, string slotName)
        {
            if (Authenticated) return "Already connected to server";

            try
            {
                CreateSession(hostName, port);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
                return $"Failed to create session.\nError log:\n{e}";
            }

            LoginResult result;

            try
            {
                result = Session.TryConnectAndLogin(
                    "Rain World",
                    slotName,
                    ItemsHandlingFlags.AllItems,
                    new Version(APVersion),
                    new string[] { "" });
            }
            catch (Exception e)
            {
                result = new LoginFailure(e.GetBaseException().Message);
                Plugin.Log.LogError(e);
            }

            if (!result.Successful)
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMessage = $"Failed to connect to {hostName}:{port} as {slotName}";
                foreach (string error in failure.Errors)
                {
                    errorMessage += $"\n\t{error}";
                }
                foreach (ConnectionRefusedError error in failure.ErrorCodes)
                {
                    errorMessage += $"\n\t{error}";
                }

                Plugin.Log.LogError(errorMessage);
                // TODO: tell the player connection failed
                Authenticated = false;
                return errorMessage;
            }

            Authenticated = true;
            HasConnected = true;
            LoginSuccessful loginSuccess = (LoginSuccessful)result;
            return $"Successfully connected to {hostName}:{port} as {slotName}!";
        }
    }
}
