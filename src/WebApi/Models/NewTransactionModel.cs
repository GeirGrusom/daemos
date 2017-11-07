// <copyright file="NewTransactionModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi.Models
{
    using System;
    using System.Dynamic;
    using Newtonsoft.Json;

    public class NewTransactionModel
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("payload")]
        public ExpandoObject Payload { get; set; }

        [JsonProperty("expires")]
        public string Expires { get; set; }

        [JsonProperty("script")]
        public string Script { get; set; }

        [JsonProperty("handler")]
        public string Handler { get; set; }
    }
}