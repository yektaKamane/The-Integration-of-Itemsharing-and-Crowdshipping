﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osrm.Client.Models
{
    public class TableRequest : BaseRequest
    {
        public TableRequest()
        {
            Sources = new uint[0];
            Destinations = new uint[0];
        }

        /// <summary>
        /// Use location with given index as source.
        /// {index};{index}[;{index} ...] or all (default)
        /// </summary>
        public uint[] Sources { get; set; }

        /// <summary>
        /// Use location with given index as destination.
        /// {index};{index}[;{index} ...] or all (default)
        /// </summary>
        public uint[] Destinations { get; set; }

        public override List<Tuple<string, string>> UrlParams
        {
            get
            {
                var urlParams = new List<Tuple<string, string>>(BaseUrlParams);

                urlParams
                    .AddParams("sources", Sources.Select(x => x.ToString()).ToArray())
                    .AddParams("destinations", Destinations.Select(x => x.ToString()).ToArray());

                //    .AddStringParameter("z", Zoom.ToString(), () => Zoom != DefaultZoom)
                //    .AddBoolParameter("alt", Alternative, true)
                //    .AddBoolParameter("geometry", Geometry, true)
                //    .AddBoolParameter("compression", Compression, true)
                //    .AddBoolParameter("uturns", UTurns, false)
                //    .AddBoolParameter("u", UTurnAtTheVia, false)
                //    .AddStringParameter("hint", Hint)
                //    .AddStringParameter("checksum", Checksum);

                return urlParams;
            }
        }
    }
}