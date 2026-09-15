// Tiện ích storefront: thêm vào giỏ bằng AJAX, toast thông báo
window.VT = (function () {
  function token() {
    const el = document.querySelector('#af-form input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
  }

  function toast(message, ok) {
    const area = document.getElementById('toast-area');
    if (!area) { alert(message); return; }
    const el = document.createElement('div');
    el.className = 'toast align-items-center text-white ' + (ok ? 'bg-success' : 'bg-danger') + ' border-0';
    el.setAttribute('role', 'alert');
    el.innerHTML = '<div class="d-flex"><div class="toast-body">' + message + '</div>' +
      '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
    area.appendChild(el);
    const t = new bootstrap.Toast(el, { delay: 3000 });
    t.show();
    el.addEventListener('hidden.bs.toast', () => el.remove());
  }

  async function addToCart(variantId, quantity) {
    const body = new URLSearchParams({ variantId, quantity: quantity || 1 });
    try {
      const res = await fetch('/gio-hang/them', {
        method: 'POST',
        headers: { 'RequestVerificationToken': token(), 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' },
        body
      });
      const data = await res.json();
      toast(data.message, data.success);
      const badge = document.getElementById('cart-count');
      if (badge) { badge.textContent = data.count; badge.classList.toggle('d-none', data.count === 0); }
      return data;
    } catch (e) {
      toast('Không thể thêm vào giỏ, vui lòng thử lại.', false);
      return { success: false };
    }
  }

  async function toggleWishlist(productId) {
    try {
      const res = await fetch('/yeu-thich/toggle', {
        method: 'POST',
        headers: { 'RequestVerificationToken': token(), 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' },
        body: new URLSearchParams({ productId })
      });
      if (res.redirected || res.status === 401) {
        window.location.href = '/dang-nhap?ReturnUrl=' + encodeURIComponent(window.location.pathname);
        return null;
      }
      const data = await res.json();
      toast(data.message, true);
      document.querySelectorAll('[data-wishlist="' + productId + '"] i').forEach(i => {
        i.classList.toggle('bi-heart-fill', data.added);
        i.classList.toggle('bi-heart', !data.added);
      });
      return data;
    } catch (e) {
      toast('Không thực hiện được, vui lòng thử lại.', false);
      return null;
    }
  }

  return { addToCart, toast, toggleWishlist };
})();
