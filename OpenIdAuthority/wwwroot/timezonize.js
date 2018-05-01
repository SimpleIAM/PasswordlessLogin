document.querySelectorAll('time').forEach(function (timeEl) {
    const date = new Date(timeEl.getAttribute('datetime'));
    timeEl.innerText = date.toLocaleString();
});
