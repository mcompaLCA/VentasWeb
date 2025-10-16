
const API_CONFIG = {
    upload: 'https://ventas-web.casadelaudio.com/subir',
    list: 'https://ventas-web.casadelaudio.com'
};

async function uploadFile(type) {
    const fileInput = document.getElementById(`file${type}`);
    const file = fileInput.files[0];

    if (!file) {
        await Swal.fire({ icon: 'warning', title: 'No se ha seleccionado ningún archivo', text: 'Por favor, selecciona un archivo para subir.' });
        return;
    }

    try {
        LoadingDialog.show("Enviando datos...");

        const formData = new FormData();
        formData.append('file', file);

        const response = await fetch(`${API_CONFIG.upload}/${type}`, {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            throw new Error('Error al subir el archivo');
        }

        LoadingDialog.hide();
        await Swal.fire({ icon: 'success', title: 'Archivo subido', text: 'El archivo se ha subido correctamente.' });

        let typeToLoad;
        switch (type) {
            case 'ae':
                typeToLoad = 'articulos-excluidos';
                break;
            case 'fe':
                typeToLoad = 'familias-excluidas';
                break;
            case 'se':
                typeToLoad = 'sucursales-excluidas';
                break;
            case 'pf':
                typeToLoad = 'precios-forzados';
                break;
        }
        loadData(typeToLoad);

        fileInput.value = '';
        
    } catch (error) {
        LoadingDialog.hide();
        await Swal.fire({ icon: 'error', title: 'Error', text: error.message });
    }
}

async function loadData(type) {
    const tableBody = document.getElementById(`table${type}`);

    try {
        LoadingDialog.show();

        const response = await fetch(`${API_CONFIG.list}/${type}`);

        if (!response.ok) {
            throw new Error('Error al cargar los datos');
        }

        const data = await response.json();

        if (!data || data.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">No hay datos disponibles</td></tr>';
            return;
        }

        // Renderizar los datos según el tipo
        tableBody.innerHTML = data.map(item => renderTableRow(type, item)).join('');

    } catch (error) {
        console.error('[v0] Error loading data:', error);
        tableBody.innerHTML = `<tr><td colspan="5" class="text-center text-danger">Error al cargar los datos: ${error.message}</td></tr>`;

    } finally
    {
        LoadingDialog.hide();
    }
}

function renderTableRow(type, item) {
    switch (type) {
        case 'articulos-excluidos':
            return `
                <tr>
                    <td>${item || '---'}</td>
                </tr>
            `;
        case 'familias-excluidas':
            return `
                <tr>
                    <td>${item || '---'}</td>
                </tr>
            `;
        case 'sucursales-excluidas':
            return `
                <tr>
                    <td>${item || '---'}</td>
                </tr>
            `;
        case 'precios-forzados':
            return `
                <tr>
                    <td>${item.sku || '---'}</td>
                    <td>${item.precioLista ?
                    item.precioLista.toLocaleString('es-AR', {
                        style: 'currency',
                        currency: 'ARS'
                    })
                    : '---'}
                    </td>
                    <td>${item.precioVenta ?
                    item.precioVenta.toLocaleString('es-AR', {
                        style: 'currency',
                        currency: 'ARS'
                    })
                    : '---'}
                    </td>
                    <td>${item.FranjaMkp ? item.FranjaMkp : '---'}</td>
                </tr>
            `;
        default:
            return '';
    }
}

document.querySelectorAll('button[data-bs-toggle="tab"]').forEach(tab => {
    tab.addEventListener('shown.bs.tab', function (event) {
        const targetId = event.target.getAttribute('data-bs-target').replace('#', '');
        loadData(targetId);
    });
});

window.addEventListener('DOMContentLoaded', () => {
    loadData('articulos-excluidos');
});


const txtFiltros = document.querySelectorAll('.txtSearchTable');
if (txtFiltros) {
    txtFiltros.forEach(txtFiltros => {
        txtFiltros.addEventListener("keyup", (e) => {
            const el = e.target;
            const normalizar = (texto) => {
                return texto
                    .toLowerCase()
                    .normalize("NFD")                       // separa letras y tildes
                    .replace(/\p{Diacritic}/gu, "")         // elimina tildes y acentos
                    .replace(/\s+/g, " ")                   // colapsa espacios múltiples
                    .trim();                                // elimina espacios al inicio y final
            };
            const criterio = normalizar(el.value);

            const contenedorfilas = el.dataset.listado;
            if (!contenedorfilas) return;

            const tableRows = document.querySelectorAll(`#${contenedorfilas} tr`);
            tableRows.forEach(row => {
                const textoFila = normalizar(row.textContent);
                row.style.display = textoFila.includes(criterio) ? "" : "none";
            });
        });
    });
}


const LoadingDialog = (() => {
    // Insertar CSS una sola vez
    const style = document.createElement('style');
    style.textContent = `
        .loading-spinner {
            width: 50px;
            height: 50px;
            margin: 100px auto 20px;
            border: 5px solid rgba(0, 0, 0, 0.1);
            border-top: 5px solid #000;
            border-radius: 50%;
            animation: spin 1s linear infinite;
        }

        @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }

        dialog#loadingDialog {
            text-align: center;
            padding: 20px;
            border: none;
            border-radius: 8px;
            min-width: 200px;
        }

        dialog#loadingDialog::backdrop {
            background-color: rgba(0, 0, 0, 0.3);
        }
    `;
    document.head.appendChild(style);

    // Crear el diÃ¡logo una vez
    const dialog = document.createElement('dialog');
    dialog.id = 'loadingDialog';
    const spinner = document.createElement('div');
    spinner.className = 'loading-spinner';
    const message = document.createElement('p');
    message.textContent = 'Cargando...';

    dialog.appendChild(spinner);
    dialog.appendChild(message);
    document.body.appendChild(dialog);

    return {
        show: (msg) => {
            message.textContent = msg || 'Cargando...';
            if (!dialog.open) dialog.showModal();
        },
        hide: () => {
            if (dialog.open) dialog.close();
        }
    };
})();