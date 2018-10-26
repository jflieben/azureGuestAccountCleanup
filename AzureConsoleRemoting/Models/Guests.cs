using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureConsoleRemoting.Models
{
    public class Guest
    {
        public string objectId { get; set; }
        public string objectType { get; set; }
        public string displayName { get; set; }
        public string userPrincipalName { get; set; }
        public string givenName { get; set; }
        public string surname { get; set; }
        public string mail { get; set; }
        public object dirSyncEnabled { get; set; }
        public object alternativeSecurityIds { get; set; }
        public List<object> signInNamesInfo { get; set; }
        public List<string> signInNames { get; set; }
        public object ownedDevices { get; set; }
        public object jobTitle { get; set; }
        public object department { get; set; }
        public string displayUserPrincipalName { get; set; }
        public bool hasThumbnail { get; set; }
        public string imageUrl { get; set; }
        public object imageDataToUpload { get; set; }
        public int source { get; set; }
        public List<string> sources { get; set; }
        public string sourceText { get; set; }
        public int userFlags { get; set; }
        public object deletionTimestamp { get; set; }
        public object permanentDeletionTime { get; set; }
        public object alternateEmailAddress { get; set; }
        public object manager { get; set; }
        public string userType { get; set; }
        public object isThumbnailUpdated { get; set; }
        public object isAuthenticationContactInfoUpdated { get; set; }
        public List<object> searchableDeviceKey { get; set; }
        public object displayEmail { get; set; }
        public string creationType { get; set; }
        public string userState { get; set; }
        public List<string> otherMails { get; set; }
    }

    public class GuestUsers
    {
        public List<Guest> items { get; set; }
        public object nextLink { get; set; }
    }
}
