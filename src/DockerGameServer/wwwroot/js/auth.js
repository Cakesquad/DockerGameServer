export async function login(dotNetRef, model) {
    const response = await fetch('/auth/login', {
        method: 'POST',
        redirect: 'manual',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json', 
        },
        body: JSON.stringify(model)
    });

    if (response.ok) {
        const result = await response.json();
        if (result.success) {
            await dotNetRef.invokeMethodAsync('OnLoginSuccess');
        }
    }
}