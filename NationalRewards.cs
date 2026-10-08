using Godot;

namespace NationalSpire;

/// <summary>
/// 国家。世界大赛开赛前公示的「国家级奖励」。
/// 设计取向来自用户要求：奖励不必真实、可以极度夸张甚至纯概念（例如"指定地点一次五十级地震"），
/// 用来体现尖塔胜者在本国的分量；数值只挂在少部分条目上，多数奖励的价值在文案与社区反响。
/// </summary>
public sealed class NationalReward
{
    public string Id { get; set; } = "";
    /// <summary>分类：资源 / 天象 / 科技 / 权力 / 概念 / 文化 / 疆域 / 荒诞。</summary>
    public string Kind { get; set; } = "概念";
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public int Treasury { get; set; }
    public int Development { get; set; }
    public int Credits { get; set; }
    public int Fans { get; set; }
    public int Weight { get; set; } = 2;
    /// <summary>玩家自行设定的条目（用于界面标注与统计）。</summary>
    public bool Custom { get; set; }

    public string Line => $"{Title}：{Detail}";

    /// <summary>弹幕用的短描述：说明过长会被弹幕层按屏宽丢弃，这里截到一句可读的长度。</summary>
    public string Description() => Detail.Length <= 26 ? Detail : Detail[..26];
}

/// <summary>某一届世界大赛公示的那一批奖励。</summary>
public sealed class RewardBatch
{
    /// <summary>赛事 Id（世界总决赛 / 世界杯各一批）。</summary>
    public string CompetitionId { get; set; } = "";
    public string Competition { get; set; } = "";
    public int Season { get; set; }
    public int AnnouncedDay { get; set; }
    /// <summary>结果揭晓日（决赛当天），公示期从 AnnouncedDay 到此日。</summary>
    public int SettleDay { get; set; }
    public List<NationalReward> Rewards { get; set; } = [];
    /// <summary>已发放给谁的国籍；空表示尚未揭晓。</summary>
    public string GrantedCountry { get; set; } = "";
    public string GrantedWinner { get; set; } = "";
    public bool Settled { get; set; }
}

/// <summary>国家奖励的配置：玩法开关、生成取向、以及玩家自定义条目。</summary>
public sealed class RewardOptions
{
    public int ConfigVersion { get; set; }
    public const int CurrentConfigVersion = 1;

    /// <summary>总开关。关闭后世界大赛不再公示国家奖励。</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>每届公示几条。</summary>
    public int PerEvent { get; set; } = 3;
    /// <summary>真实数值是否生效。关闭时奖励只作为文案与社区反响出现（纯概念模式）。</summary>
    public bool ApplyEffects { get; set; } = true;
    /// <summary>生成取向：默认 / 更离谱 / 纯概念 / 自定义。</summary>
    public string Style { get; set; } = "默认";
    /// <summary>
    /// 玩家自定义奖励，每行一条，格式：分类|奖励名|说明[|国库|发展|个人|关注]
    /// 只有"分类|奖励名|说明"三段也是合法的，数值按 0 处理。
    /// </summary>
    public string CustomLines { get; set; } = "";

    public const int MinimumPerEvent = 1, MaximumPerEvent = 8;
    public static readonly string[] Styles = ["默认", "更离谱", "纯概念", "自定义"];

    public RewardOptions Normalized()
    {
        if (ConfigVersion < CurrentConfigVersion) ConfigVersion = CurrentConfigVersion;
        PerEvent = Math.Clamp(PerEvent, MinimumPerEvent, MaximumPerEvent);
        if (!Styles.Contains(Style)) Style = "默认";
        CustomLines ??= "";
        return this;
    }

    /// <summary>
    /// 奖励不再占用「世界积分」这类通用成绩：世界大赛的名次只决定谁能拿到国家奖励，
    /// 不写入世界积分表。保留此常量用于界面提示，避免玩家去找已经不存在的积分设置。
    /// </summary>
    public const string NoPointsNote = "世界大赛不再结算世界积分，冠军获得的是本国执行的国家级奖励。";
}

/// <summary>
/// 国家级奖励的生成与发放。
/// 流程：世界大赛签表公布时生成一批并公示（社区+弹幕先热起来）→ 冠军产生时按冠军国籍整批发放 →
/// 写入社区记忆与里程碑，供 AI 社区内容与局内弹幕引用。
/// </summary>
public static class NationalRewards
{
    private static List<NationalReward>? _pool;

    /// <summary>奖励名单公示或发放时触发，携带名单本身与"是否已发放"。局内弹幕层据此推送奖励弹幕。</summary>
    public static event Action<RewardBatch, bool>? Announced;

    /// <summary>默认池文件（与词库放在一起，方便玩家直接改）。</summary>
    private const string PoolFile = "rewards-default.json";

    /// <summary>
    /// 读取奖励池：外部 mod 目录优先，其次是随 DLL 打包的内嵌副本；
    /// 最后追加玩家自定义条目。玩家选择"自定义"时只用自定义条目。
    /// </summary>
    public static List<NationalReward> Load()
    {
        _pool ??= Read();
        return _pool;
    }

    private static List<NationalReward> Read()
    {
        var list = new List<NationalReward>();
        string? json = ReadFile(PoolFile);
        if (json != null)
        {
            foreach (var reward in Parse(json))
            {
                reward.Id = $"d{list.Count}";
                list.Add(reward);
            }
        }
        else Diagnostics.Record("reward.pool.missing", $"未找到 {PoolFile}，国家奖励将只有玩家自定义条目。");
        Diagnostics.Record("reward.pool.loaded", $"奖励池载入 {list.Count} 条（{PoolFile}）");
        return list;
    }

    /// <summary>诊断用：报告当前能从哪读到奖励池、读到多少条。启动自检与公示时都会调用。</summary>
    public static string Describe()
    {
        var pool = Load();
        var custom = ParseCustom(new RewardOptions().CustomLines);
        return $"池 {pool.Count} 条 + 自定义 {custom.Count} 条；程序集目录 {AppContext.BaseDirectory}";
    }

    /// <summary>解析奖励池 JSON。字段缺失按默认值处理，非法条目跳过而不是整档失败。</summary>
    private static List<NationalReward> Parse(string json)
    {
        var result = new List<NationalReward>();
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("rewards", out var array) || array.ValueKind != System.Text.Json.JsonValueKind.Array) return result;
            foreach (var item in array.EnumerateArray())
            {
                string title = Text(item, "title");
                if (title.Length == 0) continue;
                string kind = Text(item, "kind");
                result.Add(new NationalReward
                {
                    Kind = KindAllowed(kind) ? kind : "概念",
                    Title = title,
                    Detail = Text(item, "detail"),
                    Treasury = Number(item, "treasury"),
                    Development = Number(item, "development"),
                    Credits = Number(item, "credits"),
                    Fans = Number(item, "fans"),
                    Weight = Math.Clamp(Number(item, "weight", 2), 1, 5)
                });
            }
        }
        catch (Exception e) { Diagnostics.Error("reward.pool.parse", e); }
        return result;
    }

    private static readonly string[] Kinds = ["资源", "天象", "科技", "权力", "概念", "文化", "疆域", "荒诞"];
    private static bool KindAllowed(string kind) => Kinds.Contains(kind);

    private static string Text(System.Text.Json.JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString()!.Trim() : "";

    private static int Number(System.Text.Json.JsonElement item, string name, int fallback = 0) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.Number ? value.GetInt32() : fallback;

    /// <summary>词库文件读取：模组目录优先（可热改），其次工程内嵌副本，与词库的读取策略一致。</summary>
    internal static string? ReadFile(string file)
    {
        // 程序集旁的 situation 目录优先，且找到就不再问 Godot。
        // 这不只是为了性能：Godot 的 ProjectSettings 只有引擎进程内才能安全初始化，
        // 脱机工具（存档检查、赛程模拟）读同一个方法时不能因此崩掉。
        try
        {
            string local = Path.Combine(AppContext.BaseDirectory, "situation", file);
            if (File.Exists(local)) return File.ReadAllText(local);
        }
        catch (Exception e) { Diagnostics.Error("reward.pool.read", e); }
        return ReadFileSlow(file);
    }

    /// <summary>回退路径：Godot 模组目录，其次随 DLL 打包的内嵌副本（发布版一定能读到）。</summary>
    private static string? ReadFileSlow(string file)
    {
        try
        {
            string enginePath = ProjectSettings.GlobalizePath("res://mods/NationalSpire/situation");
            string candidate = Path.Combine(enginePath, file);
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
        }
        catch (Exception e) { Diagnostics.Error("reward.pool.engine-path", e); }
        try
        {
            using var stream = typeof(NationalRewards).Assembly.GetManifestResourceStream("NationalSpire.Situation." + file);
            if (stream == null) return null;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception e) { Diagnostics.Error("reward.pool.embedded", e); return null; }
    }

    /// <summary>解析玩家自定义条目：每行 <c>分类|奖励名|说明[|国库|发展|个人|关注]</c>。</summary>
    public static List<NationalReward> ParseCustom(string lines)
    {
        var result = new List<NationalReward>();
        foreach (string raw in (lines ?? "").Split('\n'))
        {
            string line = raw.Trim().TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var parts = line.Split('|');
            if (parts.Length < 3) continue;
            string kind = parts[0].Trim();
            string title = parts[1].Trim();
            string detail = parts[2].Trim();
            if (title.Length == 0 || detail.Length == 0) continue;
            result.Add(new NationalReward
            {
                Id = $"c{result.Count}", Custom = true,
                Kind = KindAllowed(kind) ? kind : "概念",
                Title = title, Detail = detail,
                Treasury = Field(parts, 3), Development = Field(parts, 4), Credits = Field(parts, 5), Fans = Field(parts, 6),
                Weight = Math.Clamp(Field(parts, 7, 2), 1, 5)
            });
        }
        return result;
    }

    private static int Field(string[] parts, int index, int fallback = 0) =>
        parts.Length > index && int.TryParse(parts[index].Trim(), out int value) ? Math.Max(0, value) : fallback;

    /// <summary>按当前配置挑出候选池。</summary>
    public static List<NationalReward> Candidates(RewardOptions options)
    {
        options.Normalized();
        var custom = ParseCustom(options.CustomLines);
        if (options.Style == "自定义" && custom.Count > 0) return custom;
        var pool = Load().Concat(custom).ToList();
        if (options.Style == "纯概念") pool = pool.Where(r => r.Treasury == 0 && r.Development == 0 && r.Credits == 0 && r.Fans == 0).ToList();
        // 注意括号：&&/|| 优先级不同，写错会让这个分支恒真，"更离谱" 取向等于没生效。
        if (options.Style == "更离谱")
        {
            var absurd = pool.Where(r => r.Kind is "天象" or "荒诞" or "概念" or "疆域").ToList();
            if (absurd.Count > 0) pool = absurd.Concat(absurd).Concat(pool).ToList();
        }
        return pool.Count > 0 ? pool : Load();
    }

    /// <summary>
    /// 世界大赛签表公布时生成并公示一批奖励。
    /// <paramref name="settleDay"/> 为决赛日，用于公示期的界面文案。
    /// </summary>
    public static RewardBatch Announce(CareerData d, WorldCompetition c, int settleDay)
    {
        var options = d.Esports.Rewards.Normalized();
        var batch = new RewardBatch
        {
            CompetitionId = c.Id, Competition = c.Name, Season = c.Season,
            AnnouncedDay = d.Day, SettleDay = settleDay
        };
        if (!options.Enabled)
        {
            Diagnostics.Record("reward.announce.skip", $"{c.Id}：国家级奖励已关闭（Enabled=false）");
            return batch;
        }
        var candidates = Candidates(options);
        if (candidates.Count == 0)
        {
            Diagnostics.Record("reward.announce.empty", $"{c.Id}：候选池为空，未公示（取向={options.Style}）");
            return batch;
        }
        // 加权不放回抽取，保证一届之内不出现重复条目。
        var pool = candidates.ToList();
        int want = Math.Min(options.PerEvent, pool.Count);
        for (int i = 0; i < want; i++)
        {
            int total = pool.Sum(r => r.Weight);
            int roll = Random.Shared.Next(Math.Max(1, total));
            int cursor = 0, picked = pool.Count - 1;
            for (int index = 0; index < pool.Count; index++)
            {
                cursor += pool[index].Weight;
                if (roll < cursor) { picked = index; break; }
            }
            batch.Rewards.Add(pool[picked]);
            pool.RemoveAt(picked);
        }
        // 先移除同赛事的旧批次再写入新的；只有真的抽到条目才移除，避免抽空后把上一批也弄丢。
        d.Esports.RewardBatches.RemoveAll(b => b.CompetitionId == c.Id);
        d.Esports.RewardBatches.Add(batch);
        Diagnostics.Record("reward.announce", $"{c.Id} 公示 {batch.Rewards.Count} 项：{string.Join("；", batch.Rewards.Select(r => r.Title))}");
        Publish(d, batch, c, settled: false);
        return batch;
    }

    /// <summary>冠军产生后按冠军国籍发放整批奖励。</summary>
    public static void Settle(CareerData d, WorldCompetition c)
    {
        var batch = d.Esports.RewardBatches.FirstOrDefault(b => b.CompetitionId == c.Id);
        if (batch == null || batch.Settled || batch.Rewards.Count == 0) return;
        string winner = c.Kind == "league"
            ? c.ChampionTeam
            : c.TeamEvent ? c.Rosters.GetValueOrDefault(c.ChampionTeam, []).FirstOrDefault() ?? "" : c.ChampionId;
        string country = winner.Length == 0 ? "" : c.EntrantCountries.GetValueOrDefault(winner, "");
        if (country.Length == 0) country = EsportsWorld.CountryOf(d, winner);
        batch.Settled = true;
        batch.GrantedWinner = winner;
        batch.GrantedCountry = country;
        var options = d.Esports.Rewards.Normalized();
        bool apply = options.ApplyEffects;
        int credits = 0, fans = 0;
        foreach (var reward in batch.Rewards)
        {
            if (!apply) continue;
            var nation = d.Esports.Nations.FirstOrDefault(n => n.Country == country);
            if (nation != null) { nation.Treasury += reward.Treasury; nation.Development += reward.Development; }
            credits += reward.Credits; fans += reward.Fans;
        }
        if (winner == "player")
        {
            if (credits > 0) { d.Credits += credits; d.Life.PrizeTotal += credits; }
            if (fans > 0) d.Fans += fans;
        }
        Publish(d, batch, c, settled: true);
    }

    /// <summary>把公示/发放写进社区记忆与公告，供 AI 社区内容与弹幕取材。</summary>
    private static void Publish(CareerData d, RewardBatch batch, WorldCompetition c, bool settled)
    {
        Announced?.Invoke(batch, settled);
        string list = string.Join("；", batch.Rewards.Select(r => r.Line));
        if (!settled)
        {
            string body = $"{c.Name}开赛在即，本届冠军的国家级奖励名单先行公示，共 {batch.Rewards.Count} 项：{list}。"
                + $"这份名单只认冠军，落败者一项都拿不到。";
            CircuitLedger.Remember(d, "rewards-" + c.Id, body, []);
            CareerEngine.Publish(d, "rewards-" + c.Id, $"{c.Name}冠军奖励公示：{batch.Rewards.Count} 项国家级名目", body, "国际赛事", true);
            return;
        }
        string winner = CareerEngine.DisplayName(d, batch.GrantedWinner);
        string settledBody = $"{c.Name}落幕，{winner}夺冠，{batch.GrantedCountry}摘得本届全部国家级奖励：{list}。"
            + "奖励已由该国执行，国内舆论正在发酵。";
        CircuitLedger.Remember(d, "rewards-granted-" + c.Id, settledBody, [batch.GrantedWinner]);
        CareerEngine.Publish(d, "rewards-granted-" + c.Id, $"{batch.GrantedCountry}开始执行世界赛冠军奖励", settledBody, "国际赛事", true,
            batch.GrantedWinner.Length > 0 ? [batch.GrantedWinner] : []);
        if (batch.GrantedWinner == "player")
            EsportsWorld.Milestone(d, "rewards-" + c.Id, "国家级奖励执行者",
                $"你拿下{c.Name}冠军，{batch.GrantedCountry}开始执行为你准备的国家级奖励：{list}", 800, 200);
    }

    /// <summary>界面与提示词用的摘要：当前是否已有公示中的奖励名单。</summary>
    public static RewardBatch? Current(CareerData d) =>
        d.Esports.RewardBatches.Where(b => b.Season == d.Season).OrderByDescending(b => b.AnnouncedDay).FirstOrDefault();

    /// <summary>
    /// 补齐缺失的奖励名单：世界大赛签表已经生成、但该赛事没有对应名单时补一次公示。
    /// 覆盖"名单功能在世界杯生成之后才装上"以及旧存档的情况，避免公示栏一直空着。
    /// </summary>
    public static int Backfill(CareerData d)
    {
        if (!d.Esports.Rewards.Normalized().Enabled) return 0;
        int added = 0;
        foreach (var c in d.Esports.Competitions.Where(c => CircuitWorld.GroupFormat(c) && c.Season == d.Season))
        {
            if (d.Esports.RewardBatches.Any(b => b.CompetitionId == c.Id)) continue;
            // 只补还没打完的赛事：已经结束的届次补一份名单也没有意义。
            if (c.Finished) continue;
            int settle = c.GroupStartDay + CircuitWorld.WorldCupGroupDays + 6;
            var batch = Announce(d, c, settle);
            if (batch.Rewards.Count > 0)
            {
                added++;
                Diagnostics.Record("reward.backfill", $"{c.Id} 补发公示 {batch.Rewards.Count} 项");
            }
        }
        return added;
    }

    /// <summary>热重载：改完 rewards-default.json 后调用。</summary>
    public static void Reload() => _pool = null;

    /// <summary>池子统计，供诊断与界面显示。</summary>
    public static (int Total, int Custom, string[] Kinds) Stats(RewardOptions options)
    {
        var pool = Load();
        var custom = ParseCustom(options.CustomLines);
        return (pool.Count + custom.Count, custom.Count, Kinds);
    }
}
