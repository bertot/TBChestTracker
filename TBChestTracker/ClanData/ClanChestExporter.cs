﻿using CsvHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TBChestTracker.Managers;

namespace TBChestTracker
{
    public class ClanChestExporter
    {
        //-- build export data for CSV file. 
        public List<ChestExportData> ChestExportDataCollection { get; private set; }
        public string ExportFilename { get; private set; }
        public ExportSettings ExportSettings { get; private set; }
        private int InconsistentDates = 0;
        //private Dictionary<string, List<ClanChestData>> ClanChestDailyData;
        
        #region ChestCountExporter constructor and deconstructor
        public ClanChestExporter(string exportfilename, ExportSettings exportSettings)
        {
            ExportFilename = exportfilename;
            ExportSettings = exportSettings;
        }

        ~ClanChestExporter() {

            ExportFilename = String.Empty;
            ChestExportDataCollection.Clear();
            ChestExportDataCollection = null;
        }
        #endregion

    

        private void InitChestExportData()
        {
            ChestExportDataCollection.Clear();
            var clanmates = ClanManager.Instance.ClanmateManager.Database.Clanmates;
            var extraHeaders = ExportSettings.ExtraHeaders;
            foreach(var clanmate in clanmates)
            {
                ChestExportData chestExportData = new ChestExportData();
                chestExportData.Clanmate = clanmate.Name;
                if(extraHeaders != null &&  extraHeaders.Count > 0)
                {
                    foreach(var header in extraHeaders)
                    {
                        chestExportData.ExtraHeaders.Add(header, 0);
                    }
                }
                chestExportData.Total = 0;
                ChestExportDataCollection.Add(chestExportData); 
            }
        }
        private List<String> BuildDates()
        {
            var datesToSubtract = 0;
            var Dates = new List<string>();
            var isCustom = false;
            if (ExportSettings.DateRange == DateRangeEnum.Today)
            {
                datesToSubtract = 1;
            }
            else if (ExportSettings.DateRange == DateRangeEnum.Week)
            {
                datesToSubtract = 7;
            }
            else if (ExportSettings.DateRange == DateRangeEnum.Month)
            {
                datesToSubtract = 30;
            }
            else if (ExportSettings.DateRange == DateRangeEnum.Custom)
            {
                isCustom = true;    
                TimeSpan diff = ExportSettings.DateRangeTo - ExportSettings.DateRangeFrom;
                datesToSubtract = diff.Days;

                var fromDate = ExportSettings.DateRangeFrom;
                var toDate = ExportSettings.DateRangeTo;

                if(datesToSubtract == 0)
                {
                    var dateStr = fromDate.ToShortDateString();
                    Dates.Add(dateStr);
                }
                else
                {
                    for(var da = 0; da  < datesToSubtract; da++)
                    {
                        var d = toDate;
                        d = toDate.AddDays(-da);
                        var shortD = d.ToShortDateString();
                        Dates.Add(shortD);
                    }
                }
                
            }
            if (isCustom == false)
            {
                var Today = DateTime.Now;
                for (var d = 0; d < datesToSubtract; d++)
                {
                    var previousDate = Today;
                    previousDate = Today.AddDays(-d);
                    var shortDateString = previousDate.ToShortDateString();
                    Dates.Add(shortDateString);
                }
            }

            isCustom = false;
            return Dates;
        }

        private void BuildChestExportData(List<ClanChestData> dailyChestData, bool bUsePoints = false)
        {
            if (dailyChestData != null)
            {
                foreach (var chestexportdata in ChestExportDataCollection.ToList())
                {
                    var chestdataCollection = dailyChestData.Where(name => name.Clanmate.ToLower().Equals(chestexportdata.Clanmate.ToLower())).ToList();
                    if (chestdataCollection.Count() > 0)
                    {
                        var chestdata = chestdataCollection[0];
                        if (chestdata.chests != null)
                        {

                            foreach (var extraHeader in chestexportdata.ExtraHeaders.ToList())
                            {
                                foreach (var chest in chestdata.chests)
                                {
                                    var chestType = chest.Type;
                                    if (chestType.ToLower().Contains(extraHeader.Key.ToLower()))
                                    {
                                        var k = extraHeader.Key;
                                        chestexportdata.ExtraHeaders[k] += 1;
                                    }
                                }
                            }
                            chestexportdata.Total += bUsePoints == false ? chestdata.chests.Count() : chestdata.Points;
                        }
                        else
                        {
                            chestexportdata.Total += 0;
                        }
                    }
                }
            }
        }
        private void BuildAllChests()
        {
            var ClanChestDailyData = ClanManager.Instance.ClanChestManager.ClanChestDailyData;
            var Dates = BuildDates();

            foreach (var date in Dates)
            {
                List<ClanChestData> dailyChestData = null;
                try
                {
                    dailyChestData = ClanChestDailyData[date];
                }
                catch (Exception ex)
                {
                    InconsistentDates++;
                    var msg = $"{date} doesn't exist inside Clan Chest Data. Skipping.";
                    com.HellStormGames.Logging.Console.Write(msg, "Invalid Dates", com.HellStormGames.Logging.LogType.WARNING);
                    break;
                }

                BuildChestExportData(dailyChestData);   
               
            }

            Debug.WriteLine("Completed");
        }

        private void BuildChestsByConditions()
        {
            var ClanChestDailyData = ClanManager.Instance.ClanChestManager.FilterClanChestByConditions();

            var Dates = BuildDates();
            
            foreach(var date in  Dates)
            {
                List<ClanChestData> dailyChestData = null;
                try
                {
                    dailyChestData = ClanChestDailyData[date];
                }
                catch (Exception ex)
                {
                    InconsistentDates++;
                    var msg = $"{date} doesn't exist inside Clan Chest Data. Skipping.";
                    com.HellStormGames.Logging.Console.Write(msg, "Invalid Dates", com.HellStormGames.Logging.LogType.WARNING);
                    break;
                }

                BuildChestExportData(dailyChestData, false);
            }
        }
       
        private void BuildChestsByChestPoints()
        {
            var ClanChestDailyData = ClanManager.Instance.ClanChestManager.ClanChestDailyData;
            var Dates = BuildDates();

            foreach (var date in Dates)
            {
                List<ClanChestData> dailyChestData = null;
                try
                {
                    dailyChestData = ClanChestDailyData[date];
                }
                catch (Exception ex)
                {
                    InconsistentDates++;
                    var msg = $"{date} doesn't exist inside Clan Chest Data. Skipping.";
                    com.HellStormGames.Logging.Console.Write(msg, "Invalid Dates", com.HellStormGames.Logging.LogType.WARNING);
                    break;
                }

                BuildChestExportData(dailyChestData, true);
            }

            Debug.WriteLine("Completed");
        }

        private List<ChestExportData> SortChestData()
        {
            var result = new List<ChestExportData>();
            ClanChestDataComparator clanChestDataComparator = new ClanChestDataComparator();

            if (ExportSettings.SortOption == SortType.ASCENDING)
            {
                result = ChestExportDataCollection.OrderBy(i => i.Total).ToList();
            }
            else if(ExportSettings.SortOption == SortType.DESENDING)
            {
                result = ChestExportDataCollection.OrderByDescending(i => i.Total).ToList();
            }
            else if(ExportSettings.SortOption == SortType.NONE)
            {
                result = ChestExportDataCollection;
            }

            return result;
        }
        private void WriteFile(List<ChestExportData> chestdata)
        {
            var file = ExportFilename;

            using (var writer = new StreamWriter(file))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    var headers = new List<String>();
                    headers.Add("Clanmate");

                    if (chestdata[0].ExtraHeaders.Count > 0)
                    {

                        foreach (var extraheader in chestdata[0].ExtraHeaders)
                        {
                            headers.Add(extraheader.Key.ToString());
                        }
                    }

                    headers.Add("Total");

                    foreach (var heading in headers)
                    {
                        csv.WriteField(heading);
                    }

                    csv.NextRecord();

                    foreach (var item in chestdata)
                    {
                        csv.WriteField(item.Clanmate);

                        if (item.ExtraHeaders.Count > 0)
                        {
                            foreach (var extraHeader in item.ExtraHeaders)
                            {
                                var value = item.ExtraHeaders[extraHeader.Key];
                                csv.WriteField(value);
                            }
                        }

                        csv.WriteField(item.Total);
                        csv.NextRecord();
                    }

                }
                writer.Close();
            }
        }
        public bool Export()
        {
            var sortOptions = ExportSettings.SortOption;
            var dateTo = ExportSettings.DateRangeTo;
            var dateFrom = ExportSettings.DateRangeFrom;
            var dateRange = ExportSettings.DateRange;
            var extraHeaders = ExportSettings.ExtraHeaders;
            var extraChestPoints = ExportSettings.PointsAmount;

            var clanSettings = ClanManager.Instance.ClanChestSettings;

            ChestExportDataCollection = new List<ChestExportData>();

            //-- let's figure out the best 
            InitChestExportData();

            if(clanSettings.GeneralClanSettings.ChestOptions == ChestOptions.UseConditions)
            {
                //-- we build according to clan specified conditions.
                BuildChestsByConditions();
            
            }
            else if(clanSettings.GeneralClanSettings.ChestOptions == ChestOptions.UsePoints)
            {
                //--- we build chest points 
                BuildChestsByChestPoints();
            }
            else if(clanSettings.GeneralClanSettings.ChestOptions == ChestOptions.None)
            {
                //-- we build everything. 
                BuildAllChests();
            }

            var sortedChestExportData = SortChestData();

            try
            {
                WriteFile(sortedChestExportData);
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                {
                    MessageBox.Show($"Couldn't write '{ExportFilename}' because it is accessed by another program. Close the program accessing that file and try again.");
                    return false;
                }
            }
            return true;
        }
    }
}
