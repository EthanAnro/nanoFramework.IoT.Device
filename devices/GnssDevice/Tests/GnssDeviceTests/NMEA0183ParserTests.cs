// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Iot.Device.Common.GnssDevice;
using nanoFramework.TestFramework;
using System;

namespace GnssDevice.Tests
{
    [TestClass]
    public class NMEA0183ParserTests
    {
        [TestMethod]
        [DataRow("$GNGSA,A,3,65,67,80,81,82,88,66,,,,,,1.2,0.7,1.0*20", (byte)GnssOperation.Auto, (byte)Fix.Fix3D)]
        [DataRow("$GNGSA,A,2,65,67,80,81,82,88,66,,,,,,1.2,0.7,1.0*21", (byte)GnssOperation.Auto, (byte)Fix.Fix2D)]
        [DataRow("$GNGSA,A,1,65,67,80,81,82,88,66,,,,,,1.2,0.7,1.0*22", (byte)GnssOperation.Auto, (byte)Fix.NoFix)]
        [DataRow("$GNGSA,M,1,65,67,80,81,82,88,66,,,,,,1.2,0.7,1.0*2E", (byte)GnssOperation.Manual, (byte)Fix.NoFix)]
        public void ParseGngsa(string command, byte expectedMode, byte expectedFix)
        {
            // Act
            GsaData result = (GsaData)Nmea0183Parser.Parse(command);
            OutputHelper.WriteLine($"{(result == null ? "result null" : "result not null")}");
            // Assert
            Assert.AreEqual(expectedMode, (byte)result.OperationMode);
            Assert.AreEqual(expectedFix, (byte)result.Fix);
        }

        [TestMethod]
        [DataRow("$GPGLL,3723.2475,N,12158.3416,W,202725.00,A,D*70", 37.38745833333333f, -121.972359f)]
        public void ParseGpgll(string command, float expectedLatitude, float expectedLongitude)
        {
            // Act
            GllData result = (GllData)Nmea0183Parser.Parse(command);

            // Assert
            Assert.AreEqual(expectedLongitude, (float)result.Location.Longitude);
            Assert.AreEqual(expectedLatitude, (float)result.Location.Latitude);
        }

        [TestMethod]
        [DataRow("$GPGGA,002153.000,3342.6618,N,11751.3858,W,1,10,1.2,27.0,M,-34.2,M,,0000*5E", 33.7110291f, -117.85643f, 27.0f, 1.2f, 1313000d)]
        public void ParseGpgga(string command, float expectedLatitude, float expectedLongitude, float altitude, float accuracy, double time)
        {
            // Act
            GgaData result = (GgaData)Nmea0183Parser.Parse(command);

            // Assert
            Assert.AreEqual(expectedLongitude, (float)result.Location.Longitude);
            Assert.AreEqual(expectedLatitude, (float)result.Location.Latitude);
            Assert.AreEqual(altitude, (float)result.Location.Altitude);
            Assert.AreEqual(accuracy, (float)result.Location.Accuracy);
            Assert.AreEqual(time, result.Location.Timestamp.TimeOfDay.TotalMilliseconds);
        }

        [TestMethod]
        [DataRow("$GPRMC,161229.487,A,3723.2475,N,12258.3416,W,0.13,309.62,120598,,*13", 37.3874588f, -122.97236f, 0.13f, 309.62f, 2098, 05, 12, 16, 12, 29)]
        public void ParseGprmc(string command, float expectedLatitude, float expectedLongitude, float speed, float course, int yy, int mm, int dd, int hh, int min, int sec)
        {
            // Act
            RmcData result = (RmcData)Nmea0183Parser.Parse(command);

            // Assert
            Assert.AreEqual(expectedLongitude, (float)result.Location.Longitude);
            Assert.AreEqual(expectedLatitude, (float)result.Location.Latitude);
            Assert.AreEqual(speed, (float)result.Location.Speed.Knots);
            Assert.AreEqual(course, (float)result.Location.Course.Degrees);
            Assert.AreEqual(yy, result.Location.Timestamp.Year);
            Assert.AreEqual(mm, result.Location.Timestamp.Month);
            Assert.AreEqual(dd, result.Location.Timestamp.Day);
            Assert.AreEqual(hh, result.Location.Timestamp.Hour);
            Assert.AreEqual(min, result.Location.Timestamp.Minute);
            Assert.AreEqual(sec, result.Location.Timestamp.Second);
        }

        [TestMethod]
        [DataRow("$GPVTG,054.7,T,034.4,M,005.5,N,010.2,K*48", 54.7f, 5.5f)]
        public void ParseGpvtg(string command, float course, float speedKnots)
        {
            // Act
            VtgData result = (VtgData)Nmea0183Parser.Parse(command);

            // Assert
            Assert.AreEqual(course, (float)result.Location.Course.Degrees);
            Assert.AreEqual(speedKnots, (float)result.Location.Speed.Knots);
        }

        [TestMethod]
        [DataRow("GPGSV,3,1,12,02,25,259,,07,06,279,,08,73,296,,10,61,090,", "70")]
        [DataRow("GNGSA,M,1,65,67,80,81,82,88,66,,,,,,1.2,0.7,1.0", "2E")]
        [DataRow("GPGLL,3723.2475,N,12158.3416,W,202725.00,A,D", "70")]
        [DataRow("GPRMC,161229.487,A,3723.2475,N,12258.3416,W,0.13,309.62,120598,,", "13")]
        public void ComputeChecksum(string command, string checksum)
        {
            // Act
            string result = Nmea0183Parser.ComputeChecksum(command).ToString("X2");

            // Assert
            Assert.AreEqual(checksum, result);
        }

        [TestMethod]
        [DataRow("$GNGSA,A,3,65,67,80,81,82,88,66,,,,,,1.2,0.7,1.0*20", true)]
        [DataRow("$GPGSA,A,1,,,,,,,,,,,,,99.99,99.99,99.99*30", true)]
        [DataRow("$GPGSA,A,1,,,,,,,,,,,,,99.99,99.99,99.99*29", false)]
        public void ValidateChecksum(string command, bool expected)
        {
            // Act
            GsaData data = new GsaData();
            bool result = data.ValidateChecksum(command);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        [DataRow("$GNGSA,A,3,65", (int)GnssMode.Gnss)]
        [DataRow("$GPGSA,A,3,65", (int)GnssMode.Gps)]
        [DataRow("$BDGSA,A,3,65", (int)GnssMode.BeiDou)]
        [DataRow("$GLGSA,A,3,65", (int)GnssMode.Glonass)]
        [DataRow("$CQGSA,A,3,65", (int)GnssMode.Qzss)]
        [DataRow("$GAGSA,A,3,65", (int)GnssMode.Galileo)]
        [DataRow("$GIGSA,A,3,65", (int)GnssMode.NavIC)]
        [DataRow("$XXGSA,A,3,65", (int)GnssMode.Other)]
        public void TestGnssMode(string command, int gnssMode)
        {
            // Act
            // Any MneaData will do as the gnss mode is in the abstract class
            var data = new GsaData();
            var gnss = data.GetGnssMode(command);

            // Assert
            Assert.AreEqual(gnssMode, (int)gnss);
        }
    }
}
