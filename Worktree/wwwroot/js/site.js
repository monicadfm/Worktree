// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const picker = document.getElementById("label-color-picker");
const hex = document.getElementById("Color");
const nameInput = document.getElementById("Name");
const preview = document.getElementById("label-preview");
const hexPattern = /^#[0-9A-Fa-f]{6}$/;

if (picker && hex && nameInput && preview) {
    const updatePreview = () => {
        const color = hexPattern.test(hex.value) ? hex.value : picker.value;
        preview.style.color = color;
        preview.style.borderColor = color;
        preview.textContent = nameInput.value.trim() || "label";
    };

    picker.addEventListener("input", () => {
        hex.value = picker.value.toUpperCase();
        updatePreview();
    });

    hex.addEventListener("input", () => {
        if (hexPattern.test(hex.value)) {
            picker.value = hex.value;
        }
        updatePreview();
    });

    nameInput.addEventListener("input", updatePreview);
}
