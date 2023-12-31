﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;


namespace PatientsMonitoring
{
    struct TimeEventInfo
    {
        public string time;
        public double temperature;
    }
    public static class PatientData
    {
        private static DataTable FetchData(string sqlquery, SqlConnection con)
        {
            DataTable patientTable = new DataTable();
            using (var da = new SqlDataAdapter(sqlquery, con))
            {
                da.FillSchema(patientTable, SchemaType.Source);
                patientTable.Columns[4].DataType = typeof(Double);
                da.Fill(patientTable);
                foreach (DataRow dr in patientTable.Rows)
                {
                    dr.BeginEdit();
                    double temperatue = (double)dr["Temperature"] / (double)10;
                    dr["Temperature"] = temperatue;
                    dr.EndEdit();
                }
            }

            return patientTable;
        }

        public static DataTable FetchDataBySubj(string subj)
        {
            DataTable patientTable = null;
            var con = new SqlConnection(@"Data Source = HRR-PC; Initial Catalog=HRR_DB; Trusted_Connection=True");
            try
            {
                con.Open();
                patientTable = FetchData("SELECT * FROM Patients Where SubjId = '" + subj + "'", con);
                con.Close();
            }
            catch
            {
            }
            return patientTable;
        }

        public static string[] GetDistinctDate(DataTable patientDT)
        {
            string eventDateTime;
            string eventDate = "";
            List<string> dateArr = new List<string>();
            int a = 0;

            foreach (DataRow dr in patientDT.Rows)
            {
                eventDateTime = dr.ItemArray[2].ToString();
                eventDate = eventDateTime.Split(' ')[0];
                dateArr.Add(eventDate);
            }
            dateArr.Add("All");
            List<string> uniqueDate = dateArr.Distinct().ToList();
            return uniqueDate.ToArray();
        }

        //WHERE CAST([Order Date] AS date) BETWEEN '2012-06-21' AND '2012-09-21'
        // 7/22/2019

        public static DataTable FetchDataBySubjAndDate(string subj, string[] date)
        {
            string sqlDate = "'" + date[0] + "'";
            for (int i = 1; i < date.Length; i++ )
                sqlDate = sqlDate + ",'" + date[i] + "'";
            DataTable patientTable = null;
            var con = new SqlConnection(@"Data Source = HRR-PC; Initial Catalog=HRR_DB; Trusted_Connection=True");
            try
            {
                //Console.WriteLine("Opening Connection");
                con.Open();
                patientTable = FetchData("SELECT * FROM Patients WHERE SubjId = '" + subj + "' AND CAST([Date_Event] AS date) in ("+sqlDate +")", con);
                con.Close();
            }
            catch
            {
            }
            return patientTable;
        }

        public static string[] GetDistinctSensors(DataTable patientDT)
        {
            string sensor;
            List<string> sensorArr = new List<string>();
            int a = 0;

            foreach (DataRow dr in patientDT.Rows)
            {
                sensor = dr.ItemArray[3].ToString();
                sensorArr.Add(sensor);
            }
            List<string> uniqueSensor = sensorArr.Distinct().ToList();
            uniqueSensor.Sort();
            return uniqueSensor.ToArray();
        }

        public static double[] GetTemperatureOfSensor(string sensor, DataTable patientDT)
        {
            List<double> temperatureSensor = new List<double>();
            DataTable sensorDT = patientDT.AsEnumerable()
            .Where(row => row.Field<String>("SensorId") == sensor)
              //.OrderBy(row => row.Field<String>("Temperature"))
              .CopyToDataTable();

            foreach (DataRow dr in sensorDT.Rows)
            {
                temperatureSensor.Add((double)dr["Temperature"]);
            }
            return temperatureSensor.ToArray();
        }

        public static List<object> GetEventsAndTemperaturesOfDate(DataTable patientDT, string date)
        {
            List<object> eventTemperatureList = new List<object>();

            DataTable dateDT = patientDT.AsEnumerable()
            .Where(row => {
                string dateEvent = row.Field<DateTime>("Date_Event").ToString().Split(' ')[0];
                int a = 10;
                return dateEvent == date;
            })
              .OrderBy(row => row.Field<DateTime>("Date_Event"))
              .CopyToDataTable();
            foreach (DataRow dr in dateDT.Rows)
            {

                TimeEventInfo teInfo;
                string eventDateTime = dr.ItemArray[2].ToString();
                string eventTime = eventDateTime.Split(' ')[1] + eventDateTime.Split(' ')[2];
                teInfo.time = eventTime;
                teInfo.temperature = (double)dr["Temperature"];
                eventTemperatureList.Add(teInfo);
            }
            return eventTemperatureList;
        }



        public static Dictionary<string, List<object>> GetTemperatureOfPatientDates(DataTable patientDT, string[] dateArr)
        {
            Dictionary<string, List<object>> dateTemperatureDict = new Dictionary<string, List<object>>();
            DataTable dateDT = patientDT.AsEnumerable()
            .Where(row => {
                string dateEvent = row.Field<DateTime>("Date_Event").ToString().Split(' ')[0];
                bool check = dateArr.Contains(dateEvent);
                if (!check)
                    check = false;
                return dateArr.Contains(dateEvent);
            })
              .OrderBy(row => row.Field<DateTime>("Date_Event"))
              .CopyToDataTable();


            for (int i = 0; i < dateArr.Length; i++)
            {
                dateTemperatureDict.Add(dateArr[i], GetEventsAndTemperaturesOfDate(dateDT, dateArr[i]));
            }
            return dateTemperatureDict;
        }


        public static Dictionary<string, double[]> CreateDictBySensorsAndTemperature(DataTable patientTable)
        {
            Dictionary<string, double[]> sensorDict = new Dictionary<string, double[]>();
            string[] sensors = GetDistinctSensors(patientTable);
            foreach (string sensor in sensors)
            {
                double[] sensorTemperature = GetTemperatureOfSensor(sensor, patientTable);
                sensorDict.Add(sensor, sensorTemperature);
            }
            return sensorDict;
        }

        public static Dictionary<string, List<object>> CreateJoinedTemperatureList(DataTable patientTable, string[] dateArr)
        {
            return GetTemperatureOfPatientDates(patientTable, dateArr);
        }



    }
}
