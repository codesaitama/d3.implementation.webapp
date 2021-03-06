﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using AppyController.DvelopService;
using AppyController.Models;
using MenuManager.HTTPServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using persol_poc_hustler.DvelopService;

namespace MenuManager.Controllers
{

    public class ProjectCallsController : ControllerBase
    {
        MethodAPIRequest methodAPIRequest = new MethodAPIRequest();
        public IConfiguration Configuration { get; set; }
        private readonly IWebHostEnvironment _env;

        public ProjectCallsController(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        [HttpPost("api/project/postproject")]
        public async Task<object> PostProject([FromBody]object data)
        {
            var endpoint = $"{Configuration["APISETTINGS:HCMMenuBuilderMicroservice"]}Projects/CreateProject";
            return await methodAPIRequest.MakeRequestAsync(endpoint, "POST", data);
        }

        [HttpPut("api/project/putproject/{projectId}")]
        public async Task<object> PutProject([FromBody]object data, string projectId)
        {
            var endpoint = $"{Configuration["APISETTINGS:HCMMenuBuilderMicroservice"]}Projects/{projectId}";
            return await methodAPIRequest.MakeRequestAsync(endpoint, "PUT", data);
        }

        [HttpPost("api/dvelop/uploadfile")]
        public string UploadDocumentFile([FromForm(Name = "file")] IFormFile file)
        {
            try
            {
                clearFolder();
                if (file != null)
                {
                    string filePathRoot = $"{_env.WebRootPath}\\Data\\D3_Uploads\\";
                    var fileName = file.FileName;

                    string[] filNameSplit = fileName.Split(".");
                    string fileExtentsion = filNameSplit.Last();

                    var uploadFile = Path.Combine(filePathRoot, fileName);
                    var newFileName = $"{generateCode(20)}.{fileExtentsion}";
                    var renameFile = $"{filePathRoot}{newFileName}";

                    using (var stream = System.IO.File.Create(uploadFile))
                    {
                        file.CopyTo(stream);
                    }

                    System.IO.File.Move(uploadFile, renameFile);
                    return JsonConvert.SerializeObject(new { fileName = newFileName, filePath = renameFile, oldFileName = fileName });
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                if (file != null)
                {
                    string filePathRoot = $"{_env.WebRootPath}\\Data\\D3_Uploads\\";
                    var fileName = file.FileName;

                    string[] filNameSplit = fileName.Split(".");
                    string fileExtentsion = filNameSplit.Last();

                    var uploadFile = Path.Combine(filePathRoot, fileName);
                    var newFileName = $"{generateCode(20)}.{fileExtentsion}";
                    var renameFile = $"{filePathRoot}{newFileName}";

                    using (var stream = System.IO.File.Create(uploadFile))
                    {
                        file.CopyTo(stream);
                    }

                    System.IO.File.Move(uploadFile, renameFile);
                    return JsonConvert.SerializeObject(new { fileName = newFileName, filePath = renameFile, oldFileName = fileName });
                }

                
            }
            return JsonConvert.SerializeObject(new { filName = "", filePath = "", oldFileName = "" });

        }

        public void clearFolder()
        {
            string filePathRoot = $"{_env.WebRootPath}\\Data\\D3_Uploads\\";

            DirectoryInfo di = new DirectoryInfo(filePathRoot);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }


        [HttpPost("api/dvelop/savefile")]
        public string PostFileToDvelop([FromBody] SaveFileData data)
        {
            if(data != null)
            {
                var response = new InitializeFileUpload(Configuration).runUpload(data.filePath, data.map);
                return response;
            }
            return "Failed";
        }


        [HttpPost("api/dvelop/searhDocument")]
        public async Task<string> searchForDocument([FromBody]SearchData data)
        {
            var baseURI = $"{Configuration["DvelopInfos:BaseURL"]}";
            var apiKey = $"{Configuration["DvelopInfos:API_Key"]}";
            var repoId = $"{Configuration["DvelopInfos:repoId"]}";

            var sessionId = new DvelopAccessHandler().Authenticate(baseURI, apiKey);

            if(sessionId != null)
            {
                var newData = await new DocumentHandlers().SearchDocument(baseURI, sessionId, repoId, data.query); //data.query
                return newData;
            }

            return String.Empty;
        }

        [HttpPost("api/dvelop/download-document")]
        public async Task<string> DownloadFile([FromBody]SearchData data)
        {
            var baseURI = $"{Configuration["DvelopInfos:BaseURL"]}";
            var apiKey = $"{Configuration["DvelopInfos:API_Key"]}";
            var repoId = $"{Configuration["DvelopInfos:repoId"]}";
            string filePathRoot = $"{_env.WebRootPath}\\Data\\D3_Downloads\\";

            var sessionId = new DvelopAccessHandler().Authenticate(baseURI, apiKey);

            if (sessionId != null)
            {
                string downloadFile = "";
                DocumentHandlers docHandler = new DocumentHandlers();

                string getDocumentInfo = await docHandler.GetDocumentInfo(baseURI, sessionId, repoId, data.query); 

                if(getDocumentInfo != "")
                {
                    downloadFile = await docHandler.DownloadDocument(baseURI, sessionId, repoId, getDocumentInfo, filePathRoot); //data.query
                    //DeleteDocumentInTimer(filePathRoot);
                }

                return downloadFile;
            }

            return String.Empty;
        }

        [HttpPost("api/dvelop/preview-document")]
        public async Task<string> PreviewFile([FromBody] SearchData data)
        {
            var baseURI = $"{Configuration["DvelopInfos:BaseURL"]}";
            var apiKey = $"{Configuration["DvelopInfos:API_Key"]}";
            var repoId = $"{Configuration["DvelopInfos:repoId"]}";
            string filePathRoot = $"{_env.WebRootPath}\\Data\\D3_Previews\\";

            var sessionId = new DvelopAccessHandler().Authenticate(baseURI, apiKey);

            if (sessionId != null)
            {
                string downloadFile = "";
                DocumentHandlers docHandler = new DocumentHandlers();

                string getDocumentInfo = await docHandler.GetDocumentInfo(baseURI, sessionId, repoId, data.query);

                if (getDocumentInfo != "")
                {
                    downloadFile = await docHandler.DownloadDocument(baseURI, sessionId, repoId, getDocumentInfo, filePathRoot); //data.query
                    //DeleteDocumentInTimer(filePathRoot);
                }

                return downloadFile;
            }

            return String.Empty;
        }


        private string generateCode(int size, bool lowerCase = false)
        {
            var builder = new StringBuilder(size);
            Random _random = new Random();

            // char is a single Unicode character  
            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26; // A...Z or a..z: length=26  

            for (var i = 0; i < size; i++)
            {
                var @char = (char)_random.Next(offset, offset + lettersOffset);
                builder.Append(@char);
            }

            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }

        
    }

    public class SearchData
    {
        public string query { get; set; }
    }

    public class SaveFileData
    {
        public string filePath { get; set; }
        public string map { get; set; }
    }
}