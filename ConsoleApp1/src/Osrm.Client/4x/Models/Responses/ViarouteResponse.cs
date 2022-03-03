﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Osrm.Client.Models
{
    [DataContract]
    public class ViarouteResponse : OsrmBaseResponse
    {
        /// <summary>
        /// Geometry of the route compressed as polyline, but with 6 decimals.
        /// </summary>
        [DataMember(Name = "route_geometry")]
        public string RouteGeometryStr { get; set; }

        /// <summary>
        /// Geometry of the route
        /// </summary>
        public Location[] RouteGeometry
        {
            get
            {
                return OsrmPolylineConverter.Decode(RouteGeometryStr)
                    .ToArray();
            }
        }

        /// <summary>
        /// This can be used to speed up incremental queries, where only a few via change
        /// </summary>
        [DataMember(Name = "hint_data")]
        protected HintData HintData { get; set; }

        /// <summary>
        /// Array containing the instructions for each route segment. Each entry is an array of the following form:
        /// [{drive instruction code}, {street name}, {length}, {location index}, {time}, {formated length}, {post-turn direction}, {post-turn azimuth}, {mode}, {pre-turn direction}, {pre-turn azimuth}]
        /// </summary>
        [DataMember(Name = "route_instructions")]
        protected string[][] RouteInstructionsArray { get; set; }

        /// <summary>
        /// Array containing the instructions for each route segment
        /// </summary>
        public RouteInstruction[] RouteInstructions
        {
            get
            {
                if (RouteInstructionsArray == null)
                    return new RouteInstruction[0];

                return RouteInstructionsArray.Select(x => new RouteInstruction(x)).ToArray();
            }
        }

        [DataMember(Name = "route_summary")]
        public RouteSummary RouteSummary { get; set; }

        [DataMember(Name = "route_name")]
        public string[] RouteName { get; set; }

        [DataMember(Name = "via_indices")]
        public int[] ViaIndices { get; set; }

        [DataMember(Name = "via_points")]
        protected double[][] ViaPointsArray { get; set; }

        public Location[] ViaPoints
        {
            get
            {
                if (ViaPointsArray == null)
                    return new Location[0];

                return ViaPointsArray.Select(x => new Location(x[0], x[1])).ToArray();
            }
        }

        ///this can be used to speed up incremental queries, where only a few via change
        //[DataMember(Name = "hint_data")]
        //protected object HintData { get; set; }

        [DataMember(Name = "found_alternative")]
        protected bool FoundAlternative { get; set; }

        /// <summary>
        /// Alternative Geometries of the route compressed as polyline, but with 6 decimals.
        /// </summary>
        [DataMember(Name = "alternative_geometries")]
        public string[] AlternativeGeometriesStr { get; set; }

        /// <summary>
        /// Alternative Geometries of the route
        /// </summary>
        public Location[][] AlternativeGeometries
        {
            get
            {
                if (AlternativeGeometriesStr == null)
                {
                    return new Location[0][];
                }

                return AlternativeGeometriesStr.Select(x => OsrmPolylineConverter.Decode(x)
                    .ToArray()).ToArray();
            }
        }

        /// <summary>
        /// Array containing the instructions for each route segment. Each entry is an array of the following form: [{drive instruction code}, {street name}, {length}, {location index}, {time}, {formated length}, {post-turn direction}, {post-turn azimuth}, {mode}, {pre-turn direction}, {pre-turn azimuth}]
        /// </summary>
        [DataMember(Name = "alternative_instructions")]
        protected string[][][] AlternativeInstructionsArray { get; set; }

        /// <summary>
        /// Array containing the instructions for each route segment
        /// </summary>
        public RouteInstruction[][] AlternativeInstructions
        {
            get
            {
                if (AlternativeInstructionsArray == null)
                    return new RouteInstruction[0][];

                return AlternativeInstructionsArray.Select(y =>
                {
                    if (RouteInstructionsArray == null)
                        return new RouteInstruction[0];

                    return y.Select(x => new RouteInstruction(x)).ToArray();
                }).ToArray();
            }
        }

        [DataMember(Name = "alternative_summaries")]
        public RouteSummary[] AlternativeSummaries { get; set; }

        [DataMember(Name = "alternative_names")]
        public string[][] AlternativeNames { get; set; }
    }
}