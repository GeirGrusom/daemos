// <copyright file="ContinueTransactionModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi.Models
{
    using Newtonsoft.Json;

    public class ContinueTransactionModel
    {
        [JsonProperty("id")]
        public object Payload { get; set; }

        [JsonProperty("expires")]
        public string Expires { get; set; }

        [JsonProperty("script")]
        public string Script { get; set; }
    }
}