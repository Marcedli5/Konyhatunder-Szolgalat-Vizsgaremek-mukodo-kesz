function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#39;");
}

async function fetchJson(url, options) {
    const response = await fetch(url, options);
    const contentType = response.headers.get("content-type") || "";
    const payload = contentType.includes("application/json")
        ? await response.json()
        : null;

    if (!response.ok) {
        const message = payload?.message || "A kérés nem sikerült.";
        throw new Error(message);
    }

    return payload;
}

async function refreshCartCount() {
    const cartCountElement = document.getElementById("cart-count");
    if (!cartCountElement) {
        return;
    }

    try {
        const cart = await fetchJson("/api/cart");
        cartCountElement.textContent = String(cart.totalQuantity ?? 0);
    } catch {
        cartCountElement.textContent = "0";
    }
}

function showInlineFeedback(message, isError = false) {
    let feedback = document.getElementById("global-inline-feedback");
    if (!feedback) {
        feedback = document.createElement("div");
        feedback.id = "global-inline-feedback";
        document.body.appendChild(feedback);
    }

    feedback.className = `identity-message ${isError ? "identity-message-error" : "identity-message-success"} global-inline-feedback`;
    feedback.textContent = message;

    window.clearTimeout(showInlineFeedback.timeoutId);
    showInlineFeedback.timeoutId = window.setTimeout(() => {
        feedback?.remove();
    }, 3500);
}

function renderCartEmptyState(message = "A kosár jelenleg üres.") {
    const cartApp = document.getElementById("cart-app");
    if (!cartApp) {
        return;
    }

    cartApp.innerHTML = `<p class="editor-item-meta">${escapeHtml(message)}</p>`;
}

async function handleAddToCartClick(event) {
    const button = event.target.closest(".js-add-to-cart");
    if (!button) {
        return;
    }

    button.disabled = true;

    try {
        await fetchJson("/api/cart/items", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                menuId: Number(button.dataset.menuId),
                deliveryDate: button.dataset.deliveryDate,
                quantity: 1
            })
        });

        await refreshCartCount();
        showInlineFeedback("A menü bekerült a kosárba.");
    } catch (error) {
        showInlineFeedback(error.message, true);
    } finally {
        button.disabled = false;
    }
}

async function renderCartPage() {
    const cartApp = document.getElementById("cart-app");
    if (!cartApp) {
        return;
    }

    const cart = await fetchJson("/api/cart");
    if (!cart.items || cart.items.length === 0) {
        renderCartEmptyState();
        await refreshCartCount();
        return;
    }

    const deliveryDateText = cart.deliveryDate
        ? new Date(cart.deliveryDate).toLocaleDateString("hu-HU")
        : "";

    cartApp.innerHTML = `
        <div class="identity-message identity-message-info">Szállítási nap: <strong>${escapeHtml(deliveryDateText)}</strong></div>
        <div class="cart-list">
            ${cart.items.map(item => `
                <article class="editor-item">
                    <div class="menu-offer-head">
                        <h3>${escapeHtml(item.displayName)}</h3>
                        <span class="menu-price">${item.totalPriceFt.toLocaleString("hu-HU")} Ft</span>
                    </div>
                    <p class="editor-item-meta">${escapeHtml(item.menuCode)} menü</p>
                    <div class="cart-item-actions">
                        <button type="button" class="btn btn-brand-secondary js-cart-quantity" data-menu-id="${item.menuId}" data-quantity="${item.quantity - 1}">-</button>
                        <span class="cart-quantity">${item.quantity} db</span>
                        <button type="button" class="btn btn-brand-secondary js-cart-quantity" data-menu-id="${item.menuId}" data-quantity="${item.quantity + 1}">+</button>
                        <button type="button" class="btn btn-danger-soft js-cart-remove" data-menu-id="${item.menuId}">Eltávolítás</button>
                    </div>
                </article>
            `).join("")}
        </div>
        <div class="editor-card cart-summary-card">
            <h3>Összesen</h3>
            <p class="cart-total">${cart.totalPriceFt.toLocaleString("hu-HU")} Ft</p>
            <textarea id="checkout-comment" class="identity-input issue-textarea" placeholder="Megjegyzés a rendeléshez (opcionális)"></textarea>
            <div class="auth-actions">
                <button type="button" id="checkout-button" class="btn btn-brand-primary">Rendelés leadása</button>
            </div>
            <div id="checkout-feedback"></div>
        </div>`;

    await refreshCartCount();
}

async function handleCartActions(event) {
    const quantityButton = event.target.closest(".js-cart-quantity");
    if (quantityButton) {
        await fetchJson(`/api/cart/items/${quantityButton.dataset.menuId}`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ quantity: Number(quantityButton.dataset.quantity) })
        });

        await renderCartPage();
        return;
    }

    const removeButton = event.target.closest(".js-cart-remove");
    if (removeButton) {
        await fetchJson(`/api/cart/items/${removeButton.dataset.menuId}`, { method: "DELETE" });
        await renderCartPage();
        return;
    }

    const checkoutButton = event.target.closest("#checkout-button");
    if (!checkoutButton) {
        return;
    }

    checkoutButton.disabled = true;
    const comment = document.getElementById("checkout-comment")?.value || "";

    try {
        const result = await fetchJson("/api/orders/checkout", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ comment })
        });

        await refreshCartCount();
        renderCartEmptyState(`A rendelés sikeresen rögzítve lett. Azonosító: #${result.orderId}`);
        showInlineFeedback("A rendelés sikeresen leadva.");
    } catch (error) {
        const feedback = document.getElementById("checkout-feedback");
        if (feedback) {
            feedback.innerHTML = `<div class="identity-message identity-message-error">${escapeHtml(error.message)}</div>`;
        } else {
            showInlineFeedback(error.message, true);
        }
    } finally {
        checkoutButton.disabled = false;
    }
}

async function setupTicketForm() {
    const ticketButton = document.getElementById("send-ticket-button");
    if (!ticketButton) {
        return;
    }

    ticketButton.addEventListener("click", async () => {
        const description = document.getElementById("ticket-description")?.value || "";
        const feedback = document.getElementById("ticket-feedback");

        ticketButton.disabled = true;

        try {
            const result = await fetchJson("/api/tickets", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ description })
            });

            if (feedback) {
                feedback.innerHTML = `<div class="identity-message identity-message-success">${escapeHtml(result.message)}</div>`;
            }

            const textarea = document.getElementById("ticket-description");
            if (textarea) {
                textarea.value = "";
            }
        } catch (error) {
            if (feedback) {
                feedback.innerHTML = `<div class="identity-message identity-message-error">${escapeHtml(error.message)}</div>`;
            } else {
                showInlineFeedback(error.message, true);
            }
        } finally {
            ticketButton.disabled = false;
        }
    });
}

document.addEventListener("DOMContentLoaded", async () => {
    document.body.addEventListener("click", async event => {
        try {
            if (event.target.closest(".js-add-to-cart")) {
                await handleAddToCartClick(event);
                return;
            }

            if (event.target.closest(".js-cart-quantity") || event.target.closest(".js-cart-remove") || event.target.closest("#checkout-button")) {
                await handleCartActions(event);
            }
        } catch (error) {
            showInlineFeedback(error.message || "Váratlan hiba történt.", true);
        }
    });

    await refreshCartCount();

    try {
        await renderCartPage();
    } catch {
        // The current page does not expose the cart app, or the user is not signed in.
    }

    await setupTicketForm();
});
