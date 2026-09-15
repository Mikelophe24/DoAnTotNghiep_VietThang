// Tiện ích chung cho trang quản trị
(function () {
  // Xác nhận trước khi submit form có data-confirm
  document.querySelectorAll('form[data-confirm]').forEach(function (form) {
    form.addEventListener('submit', function (e) {
      if (!window.confirm(form.getAttribute('data-confirm'))) e.preventDefault();
    });
  });

  // Tự sinh slug từ tên nếu slug còn trống hoặc chưa sửa tay
  var nameInput = document.querySelector('[data-slug-source]');
  var slugInput = document.querySelector('[data-slug-target]');
  if (nameInput && slugInput) {
    var touched = slugInput.value.length > 0;
    slugInput.addEventListener('input', function () { touched = slugInput.value.length > 0; });
    nameInput.addEventListener('input', function () {
      if (touched) return;
      slugInput.value = toSlug(nameInput.value);
    });
  }

  function toSlug(str) {
    return str.normalize('NFD').replace(/[̀-ͯ]/g, '')
      .replace(/đ/g, 'd').replace(/Đ/g, 'D')
      .toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
  }

  // Chọn tất cả checkbox trong nhóm
  document.querySelectorAll('[data-check-all]').forEach(function (master) {
    var group = master.getAttribute('data-check-all');
    master.addEventListener('change', function () {
      document.querySelectorAll('input[name="' + group + '"]').forEach(function (cb) { cb.checked = master.checked; });
    });
  });
})();
