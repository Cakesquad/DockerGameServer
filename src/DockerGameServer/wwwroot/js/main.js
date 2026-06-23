window.scrollToBottom = (element) => {
    element.scrollTop = element.scrollHeight;
};

window.drawSimpleLineChart = (canvasId, values) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.warn("Canvas not found:", canvasId);
        return;
    }

    const ctx = canvas.getContext("2d");
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    ctx.beginPath();
    ctx.strokeStyle = "#0d6efd";
    ctx.lineWidth = 2;

    const step = canvas.width / (values.length - 1);
    values.forEach((v, i) => {
        const x = i * step;
        const y = canvas.height - (v / 100 * canvas.height);
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
    });

    ctx.stroke();
};
