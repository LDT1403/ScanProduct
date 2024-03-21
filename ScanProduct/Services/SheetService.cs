using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using ScanProduct.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScanProduct.Services
{
    public class SheetService : ISheetService
    {
        public SheetsService API()
        {
            string[] Scopes = { SheetsService.Scope.Spreadsheets };
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
                {
                    ClientId = "250315110973-t50v9khloci087flkj052a2jjo66skj5.apps.googleusercontent.com",
                    ClientSecret = "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"
                }
            , Scopes, "user", CancellationToken.None, new FileDataStore("MyAppsToken")).Result,
                ApplicationName = "Google Sheets API .NET Quickstart",
            });
            return service;
        }
       
    }
}
