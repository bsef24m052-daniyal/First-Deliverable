// Show/hide password
function togglePassword(inputId, button) {
    const input = document.getElementById(inputId);
    const icon = button.querySelector("i");

    if (!input) return;

    if (input.type === "password") {
        input.type = "text";
        icon.className = "bi bi-eye-slash";
    } else {
        input.type = "password";
        icon.className = "bi bi-eye";
    }
}


// Show/hide dashboard balance
document.addEventListener("DOMContentLoaded", function () {
    const balance = document.getElementById("balanceValue");
    const button = document.getElementById("toggleBalance");

    if (balance && button) {
        const actualBalance = balance.textContent;
        let hidden = false;

        button.addEventListener("click", function () {
            hidden = !hidden;

            balance.textContent = hidden ? "******" : actualBalance;

            button.innerHTML = hidden
                ? '<i class="bi bi-eye"></i> Show balance'
                : '<i class="bi bi-eye-slash"></i> Hide balance';
        });
    }
});


// Send money using AJAX
document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("sendForm");

    if (!form) return;

    const message = document.getElementById("sendMessage");
    const confirmInput =
        form.querySelector('[name="confirmOverspending"]');

    async function submitSend() {
        const data = new URLSearchParams(new FormData(form));

        try {
            const response = await fetch(form.action, {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded",
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: data
            });

            const result = await response.json();

            if (result.confirmRequired) {
                const accepted = window.confirm(result.message);

                if (accepted) {
                    confirmInput.value = "true";
                    await submitSend();
                }

                return;
            }

            if (!response.ok || !result.success) {
                message.className = "alert alert-danger mt-3";
                message.textContent =
                    result.message || "Transaction failed.";

                return;
            }

            message.className = "alert alert-success mt-3";
            message.textContent = result.message;

            form.reset();
            confirmInput.value = "false";
        }
        catch {
            message.className = "alert alert-danger mt-3";
            message.textContent =
                "An unexpected error occurred.";
        }
    }

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        confirmInput.value = "false";
        submitSend();
    });
});


// Receive money using AJAX
document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("receiveForm");

    if (!form) return;

    const message = document.getElementById("receiveMessage");

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        const data = new URLSearchParams(new FormData(form));

        try {
            const response = await fetch(form.action, {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded",
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: data
            });

            const result = await response.json();

            message.className = result.success
                ? "alert alert-success mt-3"
                : "alert alert-danger mt-3";

            message.textContent = result.message;

            if (result.success) {
                form.reset();
            }
        }
        catch {
            message.className = "alert alert-danger mt-3";
            message.textContent =
                "An unexpected error occurred.";
        }
    });
});


// Filter statements using AJAX
document.addEventListener("DOMContentLoaded", function () {
    const account = document.getElementById("statementAccount");
    const period = document.getElementById("statementPeriod");
    const container = document.getElementById("statementRows");

    if (!account || !period || !container ||
        !window.loadStatementUrl) return;

    async function loadStatement() {
        const params = new URLSearchParams();

        if (account.value) {
            params.set("miniAccountId", account.value);
        }

        params.set("period", period.value);

        container.innerHTML =
            '<p class="text-muted">Loading transactions...</p>';

        try {
            const response = await fetch(
                window.loadStatementUrl + "?" + params.toString()
            );

            if (!response.ok) {
                throw new Error("Could not load statement.");
            }

            container.innerHTML = await response.text();
        }
        catch {
            container.innerHTML =
                '<div class="alert alert-danger">Could not load transactions.</div>';
        }
    }

    account.addEventListener("change", loadStatement);
    period.addEventListener("change", loadStatement);
});