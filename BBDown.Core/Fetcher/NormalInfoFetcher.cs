using BBDown.Core.Entity;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.HTTPUtil;

namespace BBDown.Core.Fetcher;

public partial class NormalInfoFetcher : IFetcher
{
    public async Task<VInfo> FetchAsync(string id)
    {
        string api = $"https://api.bilibili.com/x/web-interface/view?aid={id}";
        string json = await GetWebSourceAsync(api);
        using var infoJson = JsonDocument.Parse(json);
        var data = infoJson.RootElement.GetProperty("data");
        string title = data.GetProperty("title").ToString();
        string desc = data.GetProperty("desc").ToString();
        string pic = data.GetProperty("pic").ToString();
        var owner = data.GetProperty("owner");
        string ownerMid = owner.GetProperty("mid").ToString();
        string ownerName = owner.GetProperty("name").ToString();
        long pubTime = data.GetProperty("pubdate").GetInt64();
        bool bangumi = false;
        var bvid = data.GetProperty("bvid").ToString();
        var cid = data.GetProperty("cid").GetInt64();

        // 互动视频 1:是 0:否
        var isSteinGate = data.GetProperty("rights").GetProperty("is_stein_gate").GetInt16();

        // 分p信息
        List<Page> pagesInfo = new();
        var pages = data.GetProperty("pages").EnumerateArray().ToList();
        foreach (var page in pages)
        {
            Page p = new(page.GetProperty("page").GetInt32(),
                id,
                page.GetProperty("cid").ToString(),
                "", //epid
                page.GetProperty("part").ToString().Trim(),
                page.GetProperty("duration").GetInt32(),
                page.GetProperty("dimension").GetProperty("width").ToString() + "x" + page.GetProperty("dimension").GetProperty("height").ToString(),
                pubTime, //分p视频没有发布时间
                "",
                "",
                ownerName,
                ownerMid
            );
            //分P投稿时间
            p.ctime = page.TryGetProperty("ctime", out var ctimeElem) ? ctimeElem.GetInt64() : null;
            pagesInfo.Add(p);
        }

        if (isSteinGate == 1) // 互动视频获取分P信息
        {
            var playerSoApi = $"https://api.bilibili.com/x/player.so?bvid={bvid}&id=cid:{cid}";
            var playerSoText = await GetWebSourceAsync(playerSoApi);
            var playerSoXml = new XmlDocument();
            playerSoXml.LoadXml($"<root>{playerSoText}</root>");
                
            var interactionNode = playerSoXml.SelectSingleNode("//interaction");

            if (interactionNode is { InnerText.Length: > 0 })
            {
                var graphVersion = JsonDocument.Parse(interactionNode.InnerText).RootElement
                    .GetProperty("graph_version").GetInt64();
                var edgeInfoApi = $"https://api.bilibili.com/x/stein/edgeinfo_v2?graph_version={graphVersion}&bvid={bvid}";
                var edgeInfoJson = await GetWebSourceAsync(edgeInfoApi);
                var edgeInfoData = JsonDocument.Parse(edgeInfoJson).RootElement.GetProperty("data");
                var questions = edgeInfoData.GetProperty("edges").GetProperty("questions").EnumerateArray()
                    .ToList();
                var index = 2; // 互动视频分P索引从2开始
                foreach (var question in questions)
                {
                    var choices = question.GetProperty("choices").EnumerateArray().ToList();
                    foreach (var page in choices)
                    {
                        Page p = new(index++,
                            id,
                            page.GetProperty("cid").ToString(),
                            "", //epid
                            page.GetProperty("option").ToString().Trim(),
                            0,
                            "",
                            pubTime, //分p视频没有发布时间
                            "",
                            "",
                            ownerName,
                            ownerMid
                        );
                        pagesInfo.Add(p);
                    }
                }
            }
            else
            {
                throw new Exception("互动视频获取分P信息失败");
            }
        }

        try
        {
            if (data.GetProperty("redirect_url").ToString().Contains("bangumi"))
            {
                bangumi = true;
                string epId = EpIdRegex().Match(data.GetProperty("redirect_url").ToString()).Groups[1].Value;
                //番剧内容通常不会有分P，如果有分P则不需要epId参数
                if (pages.Count == 1)
                {
                    pagesInfo.ForEach(p => p.epid = epId);
                }
            }
        }
        catch { }

        //UGC合集信息: 只有在合集里的视频才有ugc_season字段
        int? seasonEpCount = null;
        if (data.TryGetProperty("ugc_season", out var ugcSeason) && ugcSeason.ValueKind == JsonValueKind.Object)
        {
            string? seasonTitle = ugcSeason.TryGetProperty("title", out var seasonTitleElem) ? seasonTitleElem.GetString() : null;
            string? seasonId = ugcSeason.TryGetProperty("id", out var seasonIdElem) ? seasonIdElem.ToString() : null;
            seasonEpCount = ugcSeason.TryGetProperty("ep_count", out var epCountElem) ? epCountElem.GetInt32() : null;
            pagesInfo.ForEach(p =>
            {
                p.seasonTitle = seasonTitle;
                p.seasonId = seasonId;
            });
        }

        var info = new VInfo
        {
            Title = title.Trim(),
            Desc = desc.Trim(),
            Pic = pic,
            PubTime = pubTime,
            PagesInfo = pagesInfo,
            IsBangumi = bangumi,
            IsSteinGate = isSteinGate == 1,
            Videos = data.TryGetProperty("videos", out var videosElem) ? videosElem.GetInt32() : null,
            Duration = data.TryGetProperty("duration", out var durationElem) ? durationElem.GetInt32() : null,
            Copyright = data.TryGetProperty("copyright", out var copyrightElem) ? copyrightElem.GetInt32() : null,
            IsCooperation = data.TryGetProperty("rights", out var rightsNode) && rightsNode.TryGetProperty("is_cooperation", out var coopElem) ? coopElem.GetInt32() == 1 : null,
            DanmakuCount = GetStatLong(data, "danmaku"),
            ViewCount = GetStatLong(data, "view"),
            LikeCount = GetStatLong(data, "like"),
            CoinCount = GetStatLong(data, "coin"),
            FavoriteCount = GetStatLong(data, "favorite"),
            ReplyCount = GetStatLong(data, "reply"),
            ShareCount = GetStatLong(data, "share"),
            SeasonEpCount = seasonEpCount
        };

        return info;
    }

    /// <summary>
    /// 取 data.stat 下的整数统计值; stat 缺失或该键不是数字时返回 null
    /// </summary>
    private static long? GetStatLong(JsonElement data, string name)
    {
        if (!data.TryGetProperty("stat", out var stat) || stat.ValueKind != JsonValueKind.Object) return null;
        if (!stat.TryGetProperty(name, out var elem) || elem.ValueKind != JsonValueKind.Number) return null;
        return elem.GetInt64();
    }

    [GeneratedRegex("ep(\\d+)")]
    private static partial Regex EpIdRegex();
}