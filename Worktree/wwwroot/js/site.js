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

const avatarInput = document.getElementById("AvatarFile");
const avatarRemove = document.getElementById("avatar-remove");

const setAvatarPreview = (element) => {
    element.id = "avatar-preview";
    element.className = "avatar avatar-lg";
    document.getElementById("avatar-preview").replaceWith(element);
};

if (avatarInput) {
    avatarInput.addEventListener("change", () => {
        const file = avatarInput.files[0];
        if (!file) {
            return;
        }

        const img = document.createElement("img");
        img.src = URL.createObjectURL(file);
        setAvatarPreview(img);

        document.getElementById("RemoveAvatar").checked = false;
        document.getElementById("avatar-remove-item").classList.remove("d-none");
        document.getElementById("avatar-file-name").textContent = file.name + " selected. Click Save profile to keep it.";
    });
}

if (avatarRemove) {
    avatarRemove.addEventListener("click", () => {
        const initials = document.createElement("span");
        initials.textContent = document.querySelector(".avatar-upload").dataset.initials;
        setAvatarPreview(initials);

        avatarInput.value = "";
        document.getElementById("RemoveAvatar").checked = true;
        document.getElementById("avatar-remove-item").classList.add("d-none");
        document.getElementById("avatar-file-name").textContent = "Picture will be removed. Click Save profile to confirm.";
    });
}
