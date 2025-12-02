using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Phobos.Class.Plugin.BuiltIn;

namespace Phobos.Components.Arcusrix.Desktop
{
    /// <summary>
    /// 桌面项类型
    /// </summary>
    public enum DesktopItemType
    {
        Plugin,
        Folder
    }

    /// <summary>
    /// 桌面项基类
    /// </summary>
    public class DesktopItem
    {
        public string Id { get; set; } = string.Empty;
        public DesktopItemType Type { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
    }

    /// <summary>
    /// 插件桌面项
    /// </summary>
    public class PluginDesktopItem : DesktopItem
    {
        private string _packageName = string.Empty;

        public string PackageName
        {
            get => _packageName;
            set
            {
                _packageName = value;
                Id = value; // 自动同步 Id
            }
        }

        public PluginDesktopItem()
        {
            Type = DesktopItemType.Plugin;
        }
    }

    /// <summary>
    /// 文件夹桌面项
    /// </summary>
    public class FolderDesktopItem : DesktopItem
    {
        public string Name { get; set; } = string.Empty;
        public List<string> PluginPackageNames { get; set; } = new();

        public FolderDesktopItem()
        {
            Type = DesktopItemType.Folder;
            Id = Guid.NewGuid().ToString("N");
        }
    }

    /// <summary>
    /// 桌面项 JSON 转换器
    /// </summary>
    public class DesktopItemConverter : JsonConverter<DesktopItem>
    {
        public override DesktopItem? ReadJson(JsonReader reader, Type objectType, DesktopItem? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jsonObject = JObject.Load(reader);
            var type = jsonObject["Type"]?.ToObject<DesktopItemType>();

            DesktopItem item = type switch
            {
                DesktopItemType.Plugin => new PluginDesktopItem(),
                DesktopItemType.Folder => new FolderDesktopItem(),
                _ => new PluginDesktopItem()
            };

            using (var subReader = jsonObject.CreateReader())
            {
                serializer.Populate(subReader, item);
            }
            return item;
        }

        public override void WriteJson(JsonWriter writer, DesktopItem? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            // 手动写入避免无限循环
            var jo = new JObject
            {
                ["Id"] = value.Id,
                ["Type"] = (int)value.Type,
                ["GridX"] = value.GridX,
                ["GridY"] = value.GridY
            };

            if (value is PluginDesktopItem pluginItem)
            {
                jo["PackageName"] = pluginItem.PackageName;
            }
            else if (value is FolderDesktopItem folderItem)
            {
                jo["Name"] = folderItem.Name;
                jo["PluginPackageNames"] = JArray.FromObject(folderItem.PluginPackageNames);
            }

            jo.WriteTo(writer);
        }
    }

    /// <summary>
    /// 桌面布局配置
    /// </summary>
    public class DesktopLayout
    {
        /// <summary>
        /// 布局版本
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 网格列数
        /// </summary>
        public int Columns { get; set; } = 6;

        /// <summary>
        /// 网格行数
        /// </summary>
        public int Rows { get; set; } = 4;

        /// <summary>
        /// 是否全屏模式
        /// </summary>
        public bool IsFullscreen { get; set; } = false;

        /// <summary>
        /// 桌面项列表
        /// </summary>
        [JsonConverter(typeof(DesktopItemListConverter))]
        public List<DesktopItem> Items { get; set; } = new();

        /// <summary>
        /// 文件夹列表
        /// </summary>
        public List<FolderDesktopItem> Folders { get; set; } = new();

        /// <summary>
        /// 背景图片路径
        /// </summary>
        public string BackgroundImagePath { get; set; } = string.Empty;

        /// <summary>
        /// 背景图片缩放模式
        /// </summary>
        public string BackgroundStretch { get; set; } = "UniformToFill";

        /// <summary>
        /// 背景图片透明度 (0.0 - 1.0)
        /// </summary>
        public double BackgroundOpacity { get; set; } = 1.0;
    }

    /// <summary>
    /// 桌面项列表转换器
    /// </summary>
    public class DesktopItemListConverter : JsonConverter<List<DesktopItem>>
    {
        public override List<DesktopItem>? ReadJson(JsonReader reader, Type objectType, List<DesktopItem>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return new List<DesktopItem>();

            var items = new List<DesktopItem>();
            var array = JArray.Load(reader);

            PCLoggerPlugin.Info("DesktopItemListConverter", $"[ReadJson] Parsing {array.Count} items from JSON");

            foreach (var token in array)
            {
                // 获取类型 - 可能是整数或枚举名
                var typeToken = token["Type"];
                DesktopItemType? type = null;

                if (typeToken != null)
                {
                    PCLoggerPlugin.Info("DesktopItemListConverter", $"[ReadJson] typeToken.Type = {typeToken.Type}, value = {typeToken}");

                    if (typeToken.Type == JTokenType.Integer)
                    {
                        var typeInt = typeToken.ToObject<int>();
                        type = (DesktopItemType)typeInt;
                        PCLoggerPlugin.Info("DesktopItemListConverter", $"[ReadJson] Parsed integer type: {typeInt} -> {type}");
                    }
                    else if (typeToken.Type == JTokenType.String)
                    {
                        var typeStr = typeToken.ToString();
                        if (Enum.TryParse<DesktopItemType>(typeStr, out var parsed))
                            type = parsed;
                        PCLoggerPlugin.Info("DesktopItemListConverter", $"[ReadJson] Parsed string type: {typeStr} -> {type}");
                    }
                }
                else
                {
                    PCLoggerPlugin.Warning("DesktopItemListConverter", $"[ReadJson] Type token is null for item: {token}");
                }

                PCLoggerPlugin.Info("DesktopItemListConverter", $"[ReadJson] Parsing item with Type={type}, Id={token["Id"]}");

                DesktopItem? item = type switch
                {
                    DesktopItemType.Plugin => token.ToObject<PluginDesktopItem>(),
                    DesktopItemType.Folder => token.ToObject<FolderDesktopItem>(),
                    _ => token.ToObject<PluginDesktopItem>()
                };

                if (item != null)
                {
                    PCLoggerPlugin.Info("DesktopItemListConverter", $"[ReadJson] Created item of type: {item.GetType().Name}, item.Type = {item.Type}");
                    items.Add(item);
                }
                else
                {
                    PCLoggerPlugin.Error("DesktopItemListConverter", $"[ReadJson] Failed to create item from token: {token}");
                }
            }

            PCLoggerPlugin.Info("DesktopItemListConverter", $"[ReadJson] Total items created: {items.Count}");
            return items;
        }

        public override void WriteJson(JsonWriter writer, List<DesktopItem>? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();
            foreach (var item in value)
            {
                // 手动写入每个项目避免无限循环
                var jo = new JObject
                {
                    ["Id"] = item.Id,
                    ["Type"] = (int)item.Type,
                    ["GridX"] = item.GridX,
                    ["GridY"] = item.GridY
                };

                if (item is PluginDesktopItem pluginItem)
                {
                    jo["PackageName"] = pluginItem.PackageName;
                }
                else if (item is FolderDesktopItem folderItem)
                {
                    jo["Name"] = folderItem.Name;
                    jo["PluginPackageNames"] = JArray.FromObject(folderItem.PluginPackageNames);
                }

                jo.WriteTo(writer);
            }
            writer.WriteEndArray();
        }
    }
}
