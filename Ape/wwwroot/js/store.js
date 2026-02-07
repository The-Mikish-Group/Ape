// ============================================================
// Store Module JavaScript
// ============================================================

/**
 * Add item to shopping cart via AJAX
 */
function addToCart(productId) {
    const qtyInput = document.getElementById('cartQuantity');
    const quantity = qtyInput ? parseInt(qtyInput.value) || 1 : 1;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value
        || document.querySelector('meta[name="RequestVerificationToken"]')?.content;

    fetch('/Cart/Add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token || ''
        },
        body: JSON.stringify({ productId: productId, quantity: quantity })
    })
    .then(response => response.json())
    .then(data => {
        const msgDiv = document.getElementById('cartMessage');
        if (data.success) {
            if (msgDiv) {
                msgDiv.style.display = 'block';
                msgDiv.className = 'mt-2 alert alert-success';
                msgDiv.innerHTML = '<i class="bi bi-check-circle"></i> ' + data.message;
            }
            updateCartBadge(data.cartCount);
        } else {
            if (msgDiv) {
                msgDiv.style.display = 'block';
                msgDiv.className = 'mt-2 alert alert-danger';
                msgDiv.innerHTML = '<i class="bi bi-exclamation-circle"></i> ' + data.message;
            }
        }
    })
    .catch(err => {
        console.error('Add to cart failed:', err);
        const msgDiv = document.getElementById('cartMessage');
        if (msgDiv) {
            msgDiv.style.display = 'block';
            msgDiv.className = 'mt-2 alert alert-warning';
            msgDiv.innerHTML = '<i class="bi bi-exclamation-triangle"></i> Please <a href="/Identity/Account/Login">log in</a> to add items to your cart.';
        }
    });
}

/**
 * Update cart item quantity via AJAX
 */
function updateCartQuantity(cartItemId, quantity) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    fetch('/Cart/UpdateQuantity', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token || ''
        },
        body: JSON.stringify({ cartItemId: cartItemId, quantity: quantity })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            location.reload();
        } else {
            alert(data.message);
        }
    })
    .catch(err => console.error('Update quantity failed:', err));
}

/**
 * Remove item from cart via AJAX
 */
function removeFromCart(cartItemId) {
    if (!confirm('Remove this item from your cart?')) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    fetch('/Cart/Remove', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token || ''
        },
        body: JSON.stringify({ cartItemId: cartItemId })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            location.reload();
        }
    })
    .catch(err => console.error('Remove from cart failed:', err));
}

/**
 * Update cart count badge in navbar
 */
function updateCartBadge(count) {
    const badge = document.getElementById('cartBadge');
    if (badge) {
        badge.textContent = count;
        badge.style.display = count > 0 ? 'flex' : 'none';
    }
}

/**
 * Load cart count on page load
 */
document.addEventListener('DOMContentLoaded', function () {
    const badge = document.getElementById('cartBadge');
    if (!badge) return;

    fetch('/Cart/GetCartCount')
        .then(r => r.json())
        .then(data => updateCartBadge(data.count))
        .catch(() => {});
});
