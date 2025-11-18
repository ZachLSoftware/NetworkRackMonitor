using RackMonitor.Models;
using RackMonitor.ViewModels;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Automation;

namespace RackMonitor.Data
{
    /// <summary>
    /// Acts as the single source of truth for the rack's data and state.
    /// This class is shared between the UI (via RackViewModel) and any background services.
    /// </summary>
    public class RackRepository
    {
        public event EventHandler<DeviceSavedEventArgs> DeviceSaved;
        public string saveFolder;
        private Credentials _globalCredentials = new Credentials("", "");
        public Credentials GlobalCredentials
        {
            get => _globalCredentials;
            set
            {
                if (_globalCredentials != value)
                {
                    _globalCredentials = value;
                }
            }
        }

        public RackRepository()
        {
            string cwdPath = Directory.GetCurrentDirectory();
            Debug.WriteLine(cwdPath);
            saveFolder = Path.Combine(cwdPath, "RackMonitorData");
            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }
           // string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
           // string appFolder = Path.Combine(appDataPath, "RackMonitor");
           // Directory.CreateDirectory(appFolder); // Ensure the folder exists.
           //// _saveFilePath = Path.Combine(appFolder, "rack_state.json");


            //LoadState();
        }


        ///Save and Load
        public void SaveRack(RackStateDto rackData)
        {
            string savePath = Path.Combine(saveFolder, $"{rackData.RackName}.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(rackData, options);
            File.WriteAllText(savePath, jsonString);
        }

        public RackStateDto CreateAndSaveNewRack(string rackName, bool IsFirst=false)
        {
            List<RackUnitDto> units = new List<RackUnitDto>();
            for (int i=12; i>0; i--)
            {
                units.Add(new RackUnitDto() { UnitNumber=i, Slots = new List<SlotDto>() { new SlotDto()} });
            }
            RackStateDto newDto = new RackStateDto
            {
                NumberOfUnits = 12,
                RackName = rackName,
                Units = units,
                Default = IsFirst,
            };

            SaveRack(newDto);

            return newDto;
        }

        public List<RackStateDto> LoadAllRackData()
        {
            List<RackStateDto> rackStateDtos = new List<RackStateDto>();
            LoadGlobalCredentials();
            string[] files = Directory.GetFiles(saveFolder);
            if (files == null)
            {
                return rackStateDtos;
            }
            Debug.WriteLine(saveFolder);
            foreach (string file in files)
            {
                if (file.Contains("GlobalSettings")) { continue; }
                rackStateDtos.Add(LoadState(file));
            }
            return rackStateDtos;
        }
        private RackStateDto LoadState(string fileName)
        {
            string jsonString = File.ReadAllText(fileName);
            if (string.IsNullOrWhiteSpace(jsonString)) return null;

            var rackStateDto = JsonSerializer.Deserialize<RackStateDto>(jsonString);
            return rackStateDto;
        }



        public void SaveGlobalCredentials(Credentials credentials)
        {
            GlobalSettingsDto globalSettingsDto = new GlobalSettingsDto
            {
                Username = credentials.Username,
                Password = credentials.EncryptedPassword
            };

            string savePath = Path.Combine(saveFolder, "GlobalSettings.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(globalSettingsDto, options);
            File.WriteAllText(savePath, jsonString);

        }

        public void LoadGlobalCredentials()
        {
            string savePath = Path.Combine(saveFolder, "GlobalSettings.json");
            if (!File.Exists(savePath)) { GlobalCredentials = new Credentials("", ""); SaveGlobalCredentials(GlobalCredentials);  }
            string jsonString = File.ReadAllText(savePath);
            GlobalSettingsDto settings = JsonSerializer.Deserialize<GlobalSettingsDto>(jsonString);
            GlobalCredentials = new Credentials(settings.Username, settings.Password); 

        }

        public void DeleteRack(string rackName)
        {
            var path = Path.Combine(saveFolder, $"{rackName}.json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

    }
}
