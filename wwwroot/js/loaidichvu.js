// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: AJAX cho trang Loại dịch vụ: tìm kiếm không tải lại trang, đổi trạng thái, phân trang, biểu đồ Chart.js.
(function () {
    const form = document.getElementById('formLoc');
    const vung = document.getElementById('vungDanhSach');
    const nutExcel = document.getElementById('nutExcel');
    const khungThongBao = document.getElementById('thongBaoAjax');
    const goc = location.pathname;
    let hen;
    let bdNhom, bdTrangThai;

    function queryForm() {
        return new URLSearchParams(new FormData(form)).toString();
    }

    function capNhatLinkExcel() {
        nutExcel.href = '/LoaiDichVu/XuatExcel?' + new URLSearchParams(location.search).toString();
    }

    async function taiDanhSach(url) {
        const res = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        vung.innerHTML = await res.text();
        history.replaceState(null, '', url);
        capNhatLinkExcel();
    }

    function hienThongBao(noiDung) {
        khungThongBao.innerHTML = '<div class="alert alert-success alert-dismissible">' + noiDung + '</div>';
        setTimeout(function () { khungThongBao.innerHTML = ''; }, 3000);
    }

    // 1. Tìm kiếm / lọc / sắp xếp: gõ hoặc đổi là tự cập nhật (chờ 300ms sau khi ngừng gõ)
    form.addEventListener('input', function () {
        clearTimeout(hen);
        hen = setTimeout(function () { taiDanhSach(goc + '?' + queryForm()); }, 300);
    });
    form.addEventListener('submit', function (e) {
        e.preventDefault();
        taiDanhSach(goc + '?' + queryForm());
    });

    // 2. Chuyển trang bằng AJAX, vẫn giữ từ khóa, bộ lọc, sắp xếp (nằm sẵn trong link)
    vung.addEventListener('click', function (e) {
        const a = e.target.closest('.pagination a');
        if (!a) return;
        e.preventDefault();
        if (a.closest('.disabled')) return;
        taiDanhSach(a.href);
    });

    // 3. Bật/tắt trạng thái bằng AJAX
    vung.addEventListener('submit', async function (e) {
        const f = e.target.closest('.form-doi-trang-thai');
        if (!f) return;
        e.preventDefault();
        const res = await fetch(f.action, {
            method: 'POST',
            body: new FormData(f),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });
        if (!res.ok) { location.reload(); return; }
        const kq = await res.json();
        hienThongBao(kq.thongBao);
        await taiDanhSach(goc + location.search);
        veBieuDo();
    });

    // 4. Biểu đồ Chart.js
    async function veBieuDo() {
        const res = await fetch('/LoaiDichVu/DuLieuBieuDo');
        const d = await res.json();
        if (bdNhom) bdNhom.destroy();
        if (bdTrangThai) bdTrangThai.destroy();

        bdNhom = new Chart(document.getElementById('bieuDoNhom'), {
            type: 'bar',
            data: {
                labels: d.nhom.map(function (x) { return x.ten; }),
                datasets: [{ label: 'Số loại dịch vụ', data: d.nhom.map(function (x) { return x.soLuong; }) }]
            },
            options: {
                maintainAspectRatio: false,
                plugins: { title: { display: true, text: 'Số loại dịch vụ theo nhóm' }, legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
            }
        });

        bdTrangThai = new Chart(document.getElementById('bieuDoTrangThai'), {
            type: 'doughnut',
            data: {
                labels: ['Hoạt động', 'Đã tắt'],
                datasets: [{ data: [d.hoatDong, d.daTat] }]
            },
            options: {
                maintainAspectRatio: false,
                plugins: { title: { display: true, text: 'Trạng thái loại dịch vụ' } }
            }
        });
    }

    capNhatLinkExcel();
    veBieuDo();
})();