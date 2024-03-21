using Google.Apis.Sheets.v4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanProduct.Interfaces
{
    public interface ISheetService
    {
        SheetsService API();
    }
}
