using System;
using vatsys;
using static vatsys.FDP2.FDR;

namespace vatSysApi.Common
{
    public class FDR
    {
        public FDR() { }

        public FDR(FDP2.FDR fdr)
        {
            CFL = fdr.CFLString;
            FlightRules = fdr.FlightRules;
            DepartureAirport = fdr.DepAirport;
            DepartureRunway = fdr.DepartureRunway?.Name;
            ArrivalAirport = fdr.DesAirport;
            ArrivalRunway = fdr.ArrivalRunway?.Name;
            AltAirport = fdr.AltAirport;
            Route = fdr.Route;
            Remarks = fdr.Remarks;
            Type = fdr.AircraftType;
            Wake = fdr.AircraftWake;
            Equipment = fdr.AircraftEquip;
            SurvivalEquipment = fdr.AircraftSurvEquip;
            RFL = fdr.RFL;
            EET = fdr.EET;
            if (fdr.ATD == DateTime.MaxValue) ATD = null;
            else ATD = fdr.ATD;
            ETD = fdr.ETD;
            GlobalData = fdr.GlobalOpData;
            LocalData = fdr.LocalOpData;
            State = fdr.State;
            Number = fdr.AircraftCount;
            SID = fdr.SID?.Name;
            STAR = fdr.STAR?.Name;
        }

        public string Type { get; set; }
        public string Wake { get; set; }
        public int Number { get; set; }
        public string Equipment { get; set; }
        public string SurvivalEquipment { get; set; }
        public string FlightRules { get; set; }
        public string DepartureAirport { get; set; }
        public string DepartureRunway { get; set; }
        public string SID { get; set; }
        public string CFL { get; set; }
        public string ArrivalAirport { get; set; }
        public string ArrivalRunway { get; set; }
        public string STAR { get; set; }
        public string AltAirport { get; set; }
        public int RFL { get; set; }
        public string Route { get; set; }
        public string Remarks { get; set; }
        public TimeSpan EET { get; set; }
        public DateTime? ATD { get; set; }
        public DateTime ETD { get; set; }
        public string GlobalData { get; set; }
        public string LocalData { get; set; }
        public FDRStates State { get; set; }
    }
}