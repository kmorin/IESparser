using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

/*
 * Copyright 2013 Kyle Morin
 * This file is part of IESParser.
 * 
 * IESParser is free software: you can redistribute it and/or modify it under the terms
 * of the GNU General Public License as published by the Free Software Foundation, either 
 * version 3 of the License, or (at your option) any later version.
 * 
 * IESParser is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License along with IESParser.
 * If not, see http://www.gnu.org/licenses/.
*/

namespace ConsoleApplication1
{
    class IESParser
    {
        #region Properties
        public string IESversion;
        public string Manufac;
        public string Lumcat;
        public string Luminaire;
        public string Lamp;
        public List<string> Other;
        public string Tilt;
        public int NumOfLamps;
        public int LumensPerLamp;
        public int CDMultiplier;
        public int NumVertAngles;
        public int NumHorizAngles;
        public int BallastFactor;
        public double inputWatts;
        public List<double> VertAnglesList = new List<double>();
        public List<double> HorizAnglesList = new List<double>();
        public Dictionary<Dictionary<double, double>, double> m = new Dictionary<Dictionary<double, double>, double>();
        private int horizTotalCount = 0;
        public int linecount = 0;
        string combinedString = "";
        #endregion

        ///<summary>
        /// accepts a file input (in form of string) and outputs a matrix of values in the form of
        /// Dictionary(Dictionary(double,double),double)
        /// The key of the dictionary is the grid coordinates for the (y,x) values to search for later for interpolation.
        /// The Valuestore is the actual candela value given by the IES file at the given (y,x) coordinate.
        /// 
        /// Based on test .ies files using the IESNA:LM-63-2002 format. *Have tested with many different files
        /// from many different manufactuers, but due to the exponential number of possibilities, this parser
        /// might not work 100% every time. Just use caution.
        ///</summary>
        ///<param name="iesFile">FilePath for .ies file to parse</param>
        public Dictionary<Dictionary<double, double>, double> ParseIES(string iesFile)
        {
            FileStream fs = new FileStream(iesFile, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            int count = 0;
            int res = 0;
            string readLine = "";
            while (!sr.EndOfStream)
            {
                readLine = sr.ReadLine();
                if (readLine.Contains("IESNA:"))
                {
                    IESversion = readLine;
                }
                if (readLine.Contains("[MANUFAC]"))
                {
                    Manufac = readLine;
                }
                if (readLine.Contains("[LUMCAT]"))
                {
                    Lumcat = readLine;
                }
                if (readLine.Contains("[LUMINAIRE]"))
                {
                    Luminaire = readLine;
                }
                if (readLine.Contains("[LAMP]"))
                {
                    Lamp = readLine;
                }

                if (res == 0 && int.TryParse(readLine.Substring(0, 1), out res))
                {
                    string[] firstLine = readLine.Trim().Split(' ');
                    int.TryParse(firstLine.First(), out NumOfLamps);
                    int.TryParse(firstLine[1], out LumensPerLamp);
                    int.TryParse(firstLine[3], out NumVertAngles);
                    int.TryParse(firstLine[4], out NumHorizAngles);
                }
                else if (res > 0)
                {
                    string[] line = readLine.TrimEnd().Split(' ');
                    if (line.Count() < 4 && readLine.StartsWith(" ") == false)
                        double.TryParse(line.Last(), out inputWatts);

                    if (readLine.StartsWith("0") || readLine.StartsWith(" ") && HorizAnglesList.Count < NumHorizAngles)
                    {
                        if (line.Count() > NumHorizAngles && VertAnglesList.Count < NumVertAngles)
                        {
                            foreach (string s in line)
                            {
                                double dblRes;
                                double.TryParse(s, out dblRes);
                                if (VertAnglesList.Contains(dblRes)) { }
                                else
                                    VertAnglesList.Add(dblRes);
                            }
                        }
                        if (line.Last() == "90")
                        {
                            if (line.Count() > NumHorizAngles)
                                foreach (string s in line)
                                {
                                    double dblRes;
                                    double.TryParse(s, out dblRes);
                                    if (VertAnglesList.Contains(dblRes)) { }
                                    else
                                        VertAnglesList.Add(dblRes);
                                }

                            else if (line.Count() < NumVertAngles && line.First() == "0")
                                foreach (string s in line)
                                {
                                    double dblRes;
                                    double.TryParse(s, out dblRes);
                                    HorizAnglesList.Add(dblRes);
                                }
                        }
                    }
                    else
                    {
                        if (!readLine.StartsWith("0") && NumHorizAngles > 0 && line.Last() != inputWatts.ToString())
                        {
                            if (line.Last() == "0")
                            {
                                combinedString += readLine.Trim() + "%";
                            }
                            else
                            {
                                combinedString += readLine.Trim();
                            }
                        }
                    }
                }
                count++;
            }
            
            string[] values = combinedString.Trim().Split('%');
            foreach (string v in values)
            {
                string[] cdValues = v.Trim().Split(' ');
                foreach (string value in cdValues)
                {                   
                    double dblRes;
                    if(double.TryParse(value, out dblRes))
                    {
                        Dictionary<double, double> dictKey = new Dictionary<double, double>();
                        dictKey.Add(VertAnglesList.ElementAt(linecount), HorizAnglesList.ElementAt(horizTotalCount));
                        m.Add(dictKey, dblRes);
                        linecount++;
                    }
                }
                horizTotalCount++;
                linecount = 0;
            }
            sr.Close();
            fs.Close();
            return m;
        }

        /// <summary>
        /// Results printer for the matrix. *In progress*
        /// Idea is to take the resulting Dictionary from the parser and output to a human readable table
        /// for verification, printing, etc.
        /// </summary>
        /// <param name="m">Dictionary result from the parser</param>
        /// <returns>human readable string table to printout to console or MessageBox, etc..</returns>
        public string ResultIESMatrix(Dictionary<Dictionary<double, double>, double> m)
        {
            string sx = "";
            string sy = "";
            foreach (KeyValuePair<Dictionary<double, double>, double> i in m)
            {
                double x = 0;
                double y = 0;
                foreach (KeyValuePair<double, double> pair in i.Key)
                {
                    y = pair.Key;
                    x = pair.Value;
                }
                double candelaValue = i.Value;
            }
            return "";
        }
    }
}
