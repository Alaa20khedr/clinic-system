// Egyptian Mobile Phone Unobtrusive Validation Adapter & Unicode Regex Patch
(function ($) {
    if ($ && $.validator) {
        // Fix for Unicode regex matching in jQuery unobtrusive validation
        $.validator.methods.regex = function (value, element, param) {
            if (this.optional(element)) {
                return true;
            }
            try {
                var regex = new RegExp(param, 'u');
                return regex.test(value);
            } catch (e) {
                try {
                    var fallbackRegex = new RegExp(param);
                    return fallbackRegex.test(value);
                } catch (err) {
                    return true;
                }
            }
        };

        $.validator.addMethod('egyptianphone', function (value, element) {
            if (this.optional(element)) {
                return true;
            }

            var cleaned = value.replace(/[\s\-\(\)\.\/\\]/g, '');

            if (cleaned.startsWith('+20')) {
                var rem = cleaned.substring(3);
                cleaned = '0' + (rem.startsWith('0') ? rem.substring(1) : rem);
            } else if (cleaned.startsWith('0020')) {
                var rem = cleaned.substring(4);
                cleaned = '0' + (rem.startsWith('0') ? rem.substring(1) : rem);
            } else if (cleaned.length === 12 && cleaned.startsWith('20')) {
                var rem = cleaned.substring(2);
                cleaned = '0' + (rem.startsWith('0') ? rem.substring(1) : rem);
            }

            return /^01[0125][0-9]{8}$/.test(cleaned);
        });

        if ($.validator.unobtrusive) {
            $.validator.unobtrusive.adapters.addBool('egyptianphone');
        }
    }
})(jQuery);

// Initialize UI Enhancements on DOMContentLoaded
document.addEventListener('DOMContentLoaded', function () {
    // 1. Initialize Bootstrap Toasts
    var toastElements = document.querySelectorAll('.toast');
    toastElements.forEach(function (toastEl) {
        var toast = new bootstrap.Toast(toastEl, { delay: 6000 });
        toast.show();
    });

    // 2. Initialize Bootstrap Tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // 3. Live Cairo Clinic Clock
    var clockEl = document.getElementById('clinicLiveClock');
    if (clockEl) {
        function updateCairoClock() {
            try {
                var options = {
                    timeZone: 'Africa/Cairo',
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit',
                    hour12: true
                };
                var cairoTime = new Intl.DateTimeFormat('en-US', options).format(new Date());
                clockEl.textContent = cairoTime;
            } catch (e) {
                clockEl.textContent = new Date().toLocaleTimeString();
            }
        }
        updateCairoClock();
        setInterval(updateCairoClock, 1000);
    }

    // 4. Wire reusable Confirmation Modal
    var confirmModalEl = document.getElementById('clinicConfirmModal');
    if (confirmModalEl) {
        var confirmModal = new bootstrap.Modal(confirmModalEl);
        var confirmBtn = document.getElementById('clinicConfirmModalBtn');
        var modalTitle = document.getElementById('clinicConfirmModalLabel');
        var modalBody = document.getElementById('clinicConfirmModalBody');
        var activeForm = null;

        document.querySelectorAll('form[data-confirm="true"]').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                if (form.dataset.confirmed === 'true') {
                    return;
                }

                e.preventDefault();
                activeForm = form;

                var title = form.getAttribute('data-confirm-title') || 'Confirm Action';
                var message = form.getAttribute('data-confirm-message') || 'Are you sure you want to proceed?';
                var btnClass = form.getAttribute('data-confirm-btn-class') || 'btn-danger';
                var btnText = form.getAttribute('data-confirm-btn-text') || 'Confirm';

                modalTitle.textContent = title;
                modalBody.textContent = message;
                confirmBtn.className = 'btn ' + btnClass + ' px-4 fw-semibold';
                confirmBtn.textContent = btnText;

                confirmModal.show();
            });
        });

        confirmBtn.addEventListener('click', function () {
            if (activeForm) {
                activeForm.dataset.confirmed = 'true';
                confirmModal.hide();
                activeForm.submit();
            }
        });
    }
});
