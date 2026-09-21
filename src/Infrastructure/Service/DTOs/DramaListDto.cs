// YApi QuickType插件生成，具体参考文档:https://plugins.jetbrains.com/plugin/18847-yapi-quicktype/documentation

using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickType
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class DramaListDto
    {
        public DramaListDto(long cursor, long dramaListDtoStatusCode, Extra extra, bool hasMore, string statusMsg, LogPb logPb, List<DramaList> dramaList, long statusCode)
        {
            Cursor = cursor;
            DramaListDtoStatusCode = dramaListDtoStatusCode;
            Extra = extra;
            HasMore = hasMore;
            StatusMsg = statusMsg;
            LogPb = logPb;
            DramaList = dramaList;
            StatusCode = statusCode;
        }

        [JsonProperty("cursor")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Cursor { get; set; }

        [JsonProperty("status_code")]
        public long DramaListDtoStatusCode { get; set; }

        [JsonProperty("extra")]
        public Extra Extra { get; set; }

        [JsonProperty("hasMore")]
        public bool HasMore { get; set; }

        [JsonProperty("status_msg")]
        public string StatusMsg { get; set; }

        [JsonProperty("log_pb")]
        public LogPb LogPb { get; set; }

        [JsonProperty("dramaList")]
        public List<DramaList> DramaList { get; set; }

        [JsonProperty("statusCode")]
        public long StatusCode { get; set; }
    }

    public partial class DramaList
    {
        public DramaList(Cover cover, long totalDuration, bool isLimitedFree, long numVideos, List<Theme> themes, long numWatched, string dramaId, Dictionary<string, Uri> zoomCover, string description, string dramaName)
        {
            Cover = cover;
            TotalDuration = totalDuration;
            IsLimitedFree = isLimitedFree;
            NumVideos = numVideos;
            Themes = themes;
            NumWatched = numWatched;
            DramaId = dramaId;
            ZoomCover = zoomCover;
            Description = description;
            DramaName = dramaName;
        }

        [JsonProperty("cover")]
        public Cover Cover { get; set; }

        [JsonProperty("totalDuration")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long TotalDuration { get; set; }

        [JsonProperty("isLimitedFree")]
        public bool IsLimitedFree { get; set; }

        [JsonProperty("numVideos")]
        public long NumVideos { get; set; }

        [JsonProperty("themes")]
        public List<Theme> Themes { get; set; }

        [JsonProperty("numWatched")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long NumWatched { get; set; }

        [JsonProperty("dramaID")]
        public string DramaId { get; set; }

        [JsonProperty("zoomCover")]
        public Dictionary<string, Uri> ZoomCover { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("dramaName")]
        public string DramaName { get; set; }
    }

    public partial class Cover
    {
        public Cover(List<Uri> urlList)
        {
            UrlList = urlList;
        }

        [JsonProperty("urlList")]
        public List<Uri> UrlList { get; set; }
    }

    public partial class Theme
    {
        public Theme(string tagId, string tagKey, string tagVal)
        {
            TagId = tagId;
            TagKey = tagKey;
            TagVal = tagVal;
        }

        [JsonProperty("tagID")]
        public string TagId { get; set; }

        [JsonProperty("tagKey")]
        public string TagKey { get; set; }

        [JsonProperty("tagVal")]
        public string TagVal { get; set; }
    }

    public partial class Extra
    {
        public Extra(long now, List<object> fatalItemIds, string logid)
        {
            Now = now;
            FatalItemIds = fatalItemIds;
            Logid = logid;
        }

        [JsonProperty("now")]
        public long Now { get; set; }

        [JsonProperty("fatal_item_ids")]
        public List<object> FatalItemIds { get; set; }

        [JsonProperty("logid")]
        public string Logid { get; set; }
    }

    public partial class LogPb
    {
        public LogPb(string imprId)
        {
            ImprId = imprId;
        }

        [JsonProperty("impr_id")]
        public string ImprId { get; set; }
    }

    public partial class DramaListDto
    {
        public static DramaListDto? FromJson(string json) => JsonConvert.DeserializeObject<DramaListDto>(json, QuickType.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this DramaListDto self) => JsonConvert.SerializeObject(self, QuickType.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object? ReadJson(JsonReader reader, Type t, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object? untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }
}
