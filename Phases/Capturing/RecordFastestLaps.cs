﻿// This file is part of iRacingReplayOverlay.
//
// Copyright 2014 Dean Netherton
// https://github.com/vipoo/iRacingReplayOverlay.net
//
// iRacingReplayOverlay is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// iRacingReplayOverlay is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with iRacingReplayOverlay.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using iRacingReplayOverlay.Support;

namespace iRacingReplayOverlay.Phases.Capturing
{
    public class RecordFastestLaps
    {
        public OverlayData OverlayData;

        public int[] lastDriverLaps = new int[64];
        public double[] driverLapStartTime = new double[64];

        public double fastestLapTime = double.MaxValue;

        public double? timeToNoteFastestLap = null;
        private Capturing.OverlayData.FastLap lastFastLap;
        DateTime startTime;

        public void Start()
        {
            startTime = DateTime.Now;
        }

        public void Process(iRacingSDK.DataSample data)
        {
            var timeNow = DateTime.Now - startTime;

            if (timeToNoteFastestLap != null && timeToNoteFastestLap.Value < data.Telemetry.SessionTime)
            {
                Trace.WriteLine("Showing Driver {0} recorded a new fast lap of {1}".F(lastFastLap.Driver.Name, lastFastLap.Time));

                OverlayData.FastestLaps.Add(lastFastLap);
                timeToNoteFastestLap = null;
            }

            foreach( var lap in data.Telemetry
                .CarIdxLap
                .Select((l, i) => new {CarIdx = i, Lap = l })
                .Skip(1)
                .Take(data.SessionData.DriverInfo.Drivers.Length-1))
            {
                if( lap.Lap != lastDriverLaps[lap.CarIdx])
                {
                    var lapTime = data.Telemetry.SessionTime - driverLapStartTime[lap.CarIdx];
                    driverLapStartTime[lap.CarIdx] = data.Telemetry.SessionTime;
                    lastDriverLaps[lap.CarIdx] = lap.Lap;

                    if( lap.Lap > 1 && lapTime < fastestLapTime)
                    {
                        fastestLapTime = lapTime;

                        lastFastLap = new OverlayData.FastLap
                        {
                            Time = lapTime,
                            StartTime = (int)timeNow.TotalSeconds,
                            Driver = new OverlayData.Driver
                            {
                                Name = data.SessionData.DriverInfo.Drivers[lap.CarIdx].UserName,
                                CarNumber = (int)data.SessionData.DriverInfo.Drivers[lap.CarIdx].CarNumber
                            }
                        };

                        if (timeToNoteFastestLap == null)
                            timeToNoteFastestLap = data.Telemetry.SessionTime + 20;
                        Trace.WriteLine("Driver {0} recorded a new fast lap of {1}".F(lastFastLap.Driver.Name, lastFastLap.Time));
                    }
                }
            }
        }
    }
}