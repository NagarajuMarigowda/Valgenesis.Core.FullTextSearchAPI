using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using Syncfusion.XlsIO;
using System.Reflection;
using ValGenesis.Core.FullTextSearchAPI.DbContexts;
using ValGenesis.Core.FullTextSearchAPI.Entities;

namespace ValGenesis.Core.FullTextSearchAPI.Controllers
{
    public class FilUploaderController : Controller
    {
        private readonly FullTextSearchContext _context;
        public FilUploaderController(FullTextSearchContext context)
        {
            _context = context;
        }
        [HttpPost("FileUpload")]
        public async Task<IActionResult> Index(List<IFormFile> files)
        {
            try
            {
                string text = "";
                //Getting FileName
                long size = files.Sum(f => f.Length);
                var fileName = "";

                // full path to file in temp location
                var filePath = Path.GetTempFileName();
                var fileExtension = "";

                foreach (var formFile in files)
                {
                    if (formFile.Length > 0)
                    {
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            fileName = formFile.FileName;
                            fileExtension = Path.GetExtension(formFile.FileName);
                            await formFile.CopyToAsync(stream);
                        }
                    }
                }
                if (fileExtension == ".pdf")
                {
                    #region Begin PDF
                    Assembly assembly = typeof(Program).GetTypeInfo().Assembly;
                    Stream fileStream1 = new FileStream(@"C:\\UploadFile\Dev-2705.pdf", FileMode.Open, FileAccess.Read, FileShare.Read);
                    Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    //Loads a template document
                    //Load an existing PDF
                    //PdfLoadedDocument loadedDocument2 = new PdfLoadedDocument(fileStream1);
                    PdfLoadedDocument loadedDocument = new PdfLoadedDocument(fileStream);

                    var pageCount = loadedDocument.PageCount;
                    //Load first page
                    foreach (var file in Enumerable.Range(0, pageCount - 1))
                    {
                        PdfPageBase page = loadedDocument.Pages[file];
                        //Extract text from first page
                        text += page.ExtractText(true);
                    }
                    //Close the document
                    loadedDocument.Close(true);
                    #endregion
                }
                else if (fileExtension == ".docx")
                {
                    #region Begin WORD
                    using (FileStream inputStream = new FileStream(Path.GetFullPath(filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (WordDocument document = new WordDocument(inputStream, FormatType.Docx))
                        {
                            //Gets the document text
                            text = document.GetText();
                            //Write the text collection to a text file
                            System.IO.File.WriteAllText("SampleExtract.txt", text);
                            document.Close();
                        }
                    }
                    #endregion
                }
                // Check if the file is an Excel file
                else if (fileExtension == ".xls" || fileExtension == ".xlsx" || fileExtension == ".csv")
                {
                    using (ExcelEngine excelEngine = new ExcelEngine())
                    {
                        //Instantiate the Excel application object
                        IApplication application = excelEngine.Excel;
                        Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        //Load an existing Excel file into IWorkbook
                        IWorkbook workbook = application.Workbooks.Open(fileStream, ExcelOpenType.Automatic);

                        //Get the first worksheet in workbook into IWorksheet
                        int sheetCount = workbook.Worksheets.Count;
                        for (int m = 0; m < sheetCount; m++)
                        {
                            IWorksheet worksheet = workbook.Worksheets[m];
                            //Get the row and column length
                            int rowCount = worksheet.UsedRange.Rows.Length;
                            int columnCount = worksheet.UsedRange.Columns.Length;

                            //Initialize a string array
                            string[] rowValues = new string[columnCount];
                            //Get the values in row into array, if the row is in used range
                            for (int n = 1; n <= rowCount; n++)
                            {
                                for (int i = 0; i < columnCount; i++)
                                {
                                    text += worksheet.Range[n, i + 1].Value + " ";
                                }
                            }
                        }
                    }
                }
                else
                {
                    return BadRequest(new { message = "Invalid Formate" });
                }
                //Values saved for DB
                Random rnd = new Random();
                FileContent FileContent = new FileContent();
                FileContent.Id = rnd.Next(1, 100);
                FileContent.Name = fileName;
                FileContent.Description = text;
                _context.Add(FileContent);
                _context.SaveChanges();
                return Ok(text);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [HttpGet("searchtext")]
        public async Task<IActionResult> GetFullTextSearch(string searchtext)
        {

            var npgsql = _context.FileContent
                 .Where(p => p.SearchVector.Matches(searchtext))
                 .Select(x => new { x.Id, x.Name, x.Description })
                 .ToList();

            var firstQueryResults = npgsql.ToList();
            return Ok(firstQueryResults);
        }
    }
}
