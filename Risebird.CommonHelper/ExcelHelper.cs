using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
//using OfficeOpenXml;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class ExcelHelper : IExcelHelper
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;


        public ExcelHelper(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public byte[] CreateExcelFile(List<string> candidates, string irfName)
        {
            try
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (ExcelPackage package = new ExcelPackage())
                {
                    // Add a new worksheet to the package
                    ExcelWorksheet Sheet = package.Workbook.Worksheets.Add("All Candidate Status");
                    
                    //Sheet.Cells
                    Sheet.Cells["A1:S1"].Style.Font.Bold = true;
                    Sheet.Cells["A1:S1"].Style.Font.Size = 12;
                    Sheet.Cells["A1:S1"].Style.WrapText = true;

                    int startColumn = 1; int endColumn = 5;
                    for (int a = startColumn; a <= endColumn; a++)
                    {
                        Sheet.Column(a).Width = 30;
                    }
                    Sheet.Cells[1, 1].Value = "Name";
                    Sheet.Cells[1, 2].Value = "Email";
                    Sheet.Cells[1, 3].Value = "Status";
                    Sheet.Cells[1, 4].Value = "Skill Channel";
                    Sheet.Cells[1, 5].Value = "Mobile";
                    Sheet.Cells[1, 6].Value = "Tech Score";
                    Sheet.Cells[1, 7].Value = "Resume";
                    Sheet.Cells[1, 8].Value = "Interview Date";


                    int row = 2;
                    foreach (var candidate in candidates)
                    {
                        var candidateArray = candidate.Split('|');

                        Sheet.Cells[row, 1].Value = candidateArray[0];
                        Sheet.Cells[row, 2].Value = candidateArray[1];
                        Sheet.Cells[row, 3].Value = candidateArray[2];
                        Sheet.Cells[row, 4].Value = irfName;
                        Sheet.Cells[row, 5].Value = candidateArray[3];
                        Sheet.Cells[row, 6].Value = candidateArray[4];
                        Sheet.Cells[row, 7].Value = candidateArray[5];
                        Sheet.Cells[row, 8].Value = candidateArray[6];

                        row++;
                    }

                    // Save the Excel package to a memory stream
                    using (MemoryStream stream = new MemoryStream())
                    {
                        package.SaveAs(stream);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
