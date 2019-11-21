using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace cqhttp.Cyan.ApiCall.Results {
    ///
    public class GetVersionInfoResult : Base.ApiResult {
        ///
        public GetVersionInfoResult () { }
        /// <summary>
        /// 酷Q实例的各种参数，
        /// 一般在Instance实例化的时候会自动获取，自行调用没有多大意义。
        /// </summary>
        public InstanceVersionInfo instanceVersionInfo { get; private set; }
        ///
        override public void Parse (JToken result) {
            this.PreCheck (result);
            instanceVersionInfo = raw_data.ToObject<InstanceVersionInfo> ();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    [JsonObject]
    public class InstanceVersionInfo {
        /// <summary>酷Q的版本，"pro"/"air"</summary>
        public string coolq_edition { get; private set; }
        /// <summary>cqhttp的版本号</summary>
        public string plugin_ver { get; private set; }
        /// <summary>酷 Q 根目录路径</summary>
        public string coolq_dir { get; private set; }
    }
}