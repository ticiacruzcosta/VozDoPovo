const tPrint = document.getElementById("print");
const video = document.getElementById("cameraVideo");
const canvas = document.getElementById("cameraCanvas");
const overlayCanvas = document.getElementById("overlayCanvas");
const btnEnviar = document.getElementById("enviar");
const btnDenunciarPlaca = document.getElementById("btn-denunciar-placa");

async function iniciarCamera() {
    if (!video || !navigator.mediaDevices) return;
    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: "environment" },
            audio: false
        });
        video.srcObject = stream;
    } catch (err) {
        console.error("Erro ao acessar câmera:", err);
        if (tPrint) {
            tPrint.textContent = "Câmera indisponível (" + err.name + ")";
        }
    }
}

iniciarCamera();

async function enviarFramePlaca() {
    if (!video || !canvas || !overlayCanvas) return;

    if (!video.videoWidth || !video.videoHeight) {
        if (tPrint) tPrint.textContent = "Aguarde a câmera iniciar...";
        return;
    }

    if (tPrint) tPrint.textContent = "Analisando placa...";

    const context = canvas.getContext("2d");
    const overlayCtx = overlayCanvas.getContext("2d");

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    context.drawImage(video, 0, 0, canvas.width, canvas.height);

    canvas.toBlob(async (blob) => {
        try {
            if (!blob) throw new Error("Não foi possível capturar o quadro da câmera.");

            const formData = new FormData();
            formData.append("frame", blob, "frame.jpg");

            const response = await fetch("/LerPlaca/Enviar", {
                method: "POST",
                body: formData
            });

            const data = await response.json();

            if (!response.ok || !data || !data.placa) {
                if (tPrint) tPrint.textContent = data?.mensagem || "Nenhuma placa detectada.";
                return;
            }

            const placaFormatada = data.placa.toUpperCase();
            if (tPrint) {
                tPrint.textContent = `Placa: ${placaFormatada}`;
            }

            if (btnDenunciarPlaca) {
                btnDenunciarPlaca.href = `/Denuncia/CriarDenuncia?placa=${encodeURIComponent(placaFormatada)}`;
            }

            overlayCanvas.width = video.videoWidth;
            overlayCanvas.height = video.videoHeight;
            overlayCtx.clearRect(0, 0, overlayCanvas.width, overlayCanvas.height);

            const x = data.x1 || 0;
            const y = data.y1 || 0;
            const w = (data.x2 || 0) - x;
            const h = (data.y2 || 0) - y;

            overlayCtx.lineWidth = 3;
            overlayCtx.strokeStyle = "#22C55E";
            overlayCtx.strokeRect(x, y, w, h);

            overlayCtx.fillStyle = "#22C55E";
            overlayCtx.font = "bold 20px Segoe UI, Arial";
            overlayCtx.fillText(placaFormatada, x, Math.max(24, y - 10));
        } catch (err) {
            console.error("Erro na leitura da placa:", err);
            if (tPrint) {
                tPrint.textContent = "Falha ao consultar leitor de placas.";
            }
        }
    }, "image/jpeg");
}

if (btnEnviar) {
    btnEnviar.addEventListener("click", enviarFramePlaca);
}
