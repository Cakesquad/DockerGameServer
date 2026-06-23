export async function openPickerAndUpload(savePath, mode) {
    return new Promise((resolve, reject) => {
        const input = document.createElement("input");
        input.type = "file";
        input.multiple = true;

        if (mode === "folder") {
            input.setAttribute("webkitdirectory", "");
        }

        input.onchange = async () => {
            try {
                const formData = new FormData();

                for (const file of input.files) {
                    const relativePath = file.webkitRelativePath || file.name;
                    formData.append("files", file, relativePath);
                }

                const response = await fetch(`/file/upload?path=${savePath}`, {
                    method: "POST",
                    body: formData
                });

                if (!response.ok) {
                    reject("Upload failed.")
                    return;
                }

                resolve("upload successful.");
            }
            catch (error){
                reject(error);
            }
        };

        input.click();
    });
}

export function registerConnectionHandler(dotnetRef) {
    Blazor.reconnectionHandler = {
        onConnectionDown: () => {
            dotnetRef.invokeMethodAsync("NotifyConnectionLost");
        },
        onConnectionUp: () => {
            dotnetRef.invokeMethodAsync("NotifyConnectionRestored");
        }
    };
}