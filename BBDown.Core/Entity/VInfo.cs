using static BBDown.Core.Entity.Entity;

namespace BBDown.Core.Entity;

public class VInfo
{
    /// <summary>
    /// 视频标题
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// 视频描述
    /// </summary>
    public required string Desc { get; set; }

    /// <summary>
    /// 视频封面
    /// </summary>
    public required string Pic { get; set; }

    /// <summary>
    /// 视频发布时间
    /// </summary>
    public required long PubTime { get; set; }
    public bool IsBangumi { get; set; }
    public bool IsCheese { get; set; }

    /// <summary>
    /// 番剧是否完结
    /// </summary>
    public bool IsBangumiEnd { get; set; }

    /// <summary>
    /// 视频index 用于番剧或课程判断当前选择的是第几集
    /// </summary>
    public string? Index { get; set; }

    /// <summary>
    /// 视频分P信息
    /// </summary>
    public required List<Page> PagesInfo { get; set; }

    /// <summary>
    /// 是否为互动视频
    /// </summary>
    public bool IsSteinGate { get; set; }

    /// <summary>
    /// 本次解析是否来自"系列"(series), 系列不是合集(ugc_season), 两者结构不同
    /// </summary>
    public bool IsSeries { get; set; }

    // ---- 以下为稿件级统计信息, 只有 view 接口会返回; 取不到时保持 null, 写信息文件时输出空 ----

    /// <summary>
    /// 分P总数(接口 videos 字段)
    /// </summary>
    public int? Videos { get; set; }

    /// <summary>
    /// 稿件总时长(秒, 接口 duration 字段; 多P时为各分P之和)
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// 稿件类型: 1=自制 2=转载
    /// </summary>
    public int? Copyright { get; set; }

    /// <summary>
    /// 是否为联合投稿(rights.is_cooperation)
    /// </summary>
    public bool? IsCooperation { get; set; }

    public long? DanmakuCount { get; set; }
    public long? ViewCount { get; set; }
    public long? LikeCount { get; set; }
    public long? CoinCount { get; set; }
    public long? FavoriteCount { get; set; }
    public long? ReplyCount { get; set; }
    public long? ShareCount { get; set; }

    /// <summary>
    /// 所属UGC合集的总集数(ugc_season.ep_count)
    /// </summary>
    public int? SeasonEpCount { get; set; }
}