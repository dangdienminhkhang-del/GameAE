using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Core.World
{
    /// <summary>
    /// Tiến trình khám phá của người chơi: bản đồ tự vẽ (key "map") và Đèn Hiệu đã thắp (key "beacons").
    /// Cùng mẫu với <see cref="PersonalProgress"/>:
    /// • Cập nhật bộ nhớ NGAY, lưu sau; lưu lỗi → giữ nguyên trong bộ nhớ, lần sau thử lại.
    /// • KHÔNG BAO GIỜ ném ra ngoài: đọc/lưu lỗi chỉ log cảnh báo.
    /// • Chưa đọc được save thì không ghi đè (tránh xoá bản đồ đã lưu bằng bản rỗng).
    /// • Dữ liệu đã lưu của tầng chưa nạp trong phiên này được giữ nguyên khi lưu.
    /// </summary>
    public sealed class ExplorationProgress
    {
        private readonly ISaveService _save;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, MapRevealGrid> _grids = new Dictionary<string, MapRevealGrid>();
        private readonly Dictionary<string, MapFloorEntry> _saved = new Dictionary<string, MapFloorEntry>();
        private readonly HashSet<string> _lit = new HashSet<string>();

        private bool _loaded;
        private bool _mapDirty;
        private bool _beaconsDirty;

        public ExplorationProgress(ISaveService saveService)
        {
            _save = saveService;
        }

        public bool HasUnsavedChanges => _mapDirty || _beaconsDirty;
        public bool IsLoaded => _loaded;

        /// <summary>Đọc "map" và "beacons". Lỗi → log cảnh báo, giữ dữ liệu trong bộ nhớ; không ném.</summary>
        public async Task LoadAsync()
        {
            if (_save == null) { _loaded = true; return; }

            MapData map = null;
            BeaconData beacons = null;
            bool ok = true;
            try { map = await _save.LoadAsync<MapData>(SaveKeys.Map); }
            catch (Exception e) { ok = false; Debug.LogWarning($"[Exploration] Không đọc được '{SaveKeys.Map}': {e.Message}"); }
            try { beacons = await _save.LoadAsync<BeaconData>(SaveKeys.Beacons); }
            catch (Exception e) { ok = false; Debug.LogWarning($"[Exploration] Không đọc được '{SaveKeys.Beacons}': {e.Message}"); }

            if (map != null && map.floors != null)
            {
                foreach (var entry in map.floors)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.floorId)) continue;
                    _saved[entry.floorId] = entry;
                    if (_grids.TryGetValue(entry.floorId, out var live) && live.MergeBase64(entry.bits, entry.blocksX, entry.blocksY))
                        _mapDirty = true; // bản lưu có thêm so với bộ nhớ → gộp xong thì lưu lại bản hợp nhất
                }
            }

            if (beacons != null && beacons.lit != null)
            {
                foreach (var id in beacons.lit)
                    if (!string.IsNullOrEmpty(id)) _lit.Add(id);
            }

            if (ok) _loaded = true;
        }

        /// <summary>Lưới của một tầng (tạo nếu chưa có, gộp phần đã lưu). Giới hạn thế giới của tầng do nơi gọi cấp.</summary>
        public MapRevealGrid GetGrid(string floorId, float minX, float minY, float width, float height)
        {
            if (string.IsNullOrEmpty(floorId)) throw new ArgumentException("floorId rỗng", nameof(floorId));
            if (_grids.TryGetValue(floorId, out var existing)) return existing;

            var grid = new MapRevealGrid(minX, minY, width, height);
            if (_saved.TryGetValue(floorId, out var entry)) grid.MergeBase64(entry.bits, entry.blocksX, entry.blocksY);
            _grids[floorId] = grid;
            return grid;
        }

        /// <summary>Vẽ bản đồ quanh một điểm. Trả true nếu có khối mới (đã đánh dấu cần lưu).</summary>
        public bool Reveal(string floorId, float x, float y, float radius)
        {
            if (!_grids.TryGetValue(floorId, out var grid)) return false;
            if (!grid.RevealCircle(x, y, radius)) return false;
            _mapDirty = true;
            return true;
        }

        public bool IsLit(string beaconId) => !string.IsNullOrEmpty(beaconId) && _lit.Contains(beaconId);

        /// <summary>Thắp một Đèn Hiệu. Trả true nếu đây là lần đầu (để báo "Đã thắp...").</summary>
        public bool Light(string beaconId)
        {
            if (string.IsNullOrEmpty(beaconId) || !_lit.Add(beaconId)) return false;
            _beaconsDirty = true;
            return true;
        }

        public IReadOnlyCollection<string> LitBeacons => _lit;

        public async Task SaveIfDirtyAsync()
        {
            if (_save == null || !HasUnsavedChanges) return;

            await _gate.WaitAsync();
            try
            {
                if (!HasUnsavedChanges) return;
                if (!_loaded) await LoadAsync();
                if (!_loaded) return; // chưa đọc được → không ghi đè

                if (_mapDirty)
                {
                    var data = new MapData();
                    var written = new HashSet<string>();
                    foreach (var kv in _grids)
                    {
                        var g = kv.Value;
                        data.floors.Add(new MapFloorEntry { floorId = kv.Key, blocksX = g.BlocksX, blocksY = g.BlocksY, bits = g.ToBase64() });
                        written.Add(kv.Key);
                    }
                    foreach (var kv in _saved)
                        if (!written.Contains(kv.Key)) data.floors.Add(kv.Value);

                    _mapDirty = false; // bỏ cờ trước khi chờ: thay đổi mới trong lúc lưu sẽ bật lại
                    try
                    {
                        await _save.SaveAsync(SaveKeys.Map, data);
                        foreach (var e in data.floors) _saved[e.floorId] = e;
                    }
                    catch (Exception e)
                    {
                        _mapDirty = true;
                        Debug.LogWarning($"[Exploration] Chưa lưu được '{SaveKeys.Map}' (giữ trong bộ nhớ): {e.Message}");
                    }
                }

                if (_beaconsDirty)
                {
                    var data = new BeaconData();
                    data.lit.AddRange(_lit);
                    _beaconsDirty = false;
                    try { await _save.SaveAsync(SaveKeys.Beacons, data); }
                    catch (Exception e)
                    {
                        _beaconsDirty = true;
                        Debug.LogWarning($"[Exploration] Chưa lưu được '{SaveKeys.Beacons}' (giữ trong bộ nhớ): {e.Message}");
                    }
                }
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
