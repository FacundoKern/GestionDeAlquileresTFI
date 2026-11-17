// Panel del asistente (mejora progresiva): manda la pregunta por AJAX al mismo endpoint que usa la página
// /Asistente sin JavaScript, y muestra la respuesta como texto (nunca como HTML).
(function () {
    'use strict';

    var formulario = document.querySelector('[data-asistente-formulario]');
    var mensajes = document.querySelector('[data-asistente-mensajes]');
    if (!formulario || !mensajes || !window.fetch || !window.FormData) {
        return;
    }

    var campo = formulario.querySelector('textarea');

    formulario.addEventListener('submit', function (evento) {
        evento.preventDefault();
        if (formulario.getAttribute('data-consultando') === 'true') {
            return;
        }

        var pregunta = campo.value.trim();
        if (!pregunta) {
            campo.focus();
            return;
        }

        var datos = new FormData(formulario);
        agregarMensaje('pregunta', 'Vos', pregunta);
        var esperando = agregarMensaje('estado', 'Asistente', 'Pensando la respuesta…');
        formulario.setAttribute('data-consultando', 'true');
        formulario.setAttribute('aria-busy', 'true');

        fetch(formulario.action, {
            method: 'POST',
            body: datos,
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        })
            .then(function (respuesta) {
                if (respuesta.status === 429) {
                    return { exito: false, texto: 'Hiciste muchas consultas seguidas. Esperá un minuto y probá de nuevo.' };
                }
                if (!respuesta.ok) {
                    throw new Error('Respuesta ' + respuesta.status);
                }
                return respuesta.json();
            })
            .then(function (resultado) {
                esperando.remove();
                agregarMensaje(resultado.exito ? 'respuesta' : 'error', 'Asistente', resultado.texto);
                if (resultado.exito) {
                    campo.value = '';
                }
            })
            .catch(function () {
                esperando.remove();
                agregarMensaje('error', 'Asistente', 'No se pudo consultar al asistente. Revisá tu conexión o volvé a iniciar sesión.');
            })
            .finally(function () {
                formulario.removeAttribute('data-consultando');
                formulario.removeAttribute('aria-busy');
                formulario.removeAttribute('data-enviando'); // la marca anti doble envío de site.js
                campo.focus();
            });
    });

    function agregarMensaje(tipo, autor, texto) {
        var mensaje = document.createElement('p');
        mensaje.className = 'asistente-mensaje asistente-' + tipo;
        var quien = document.createElement('span');
        quien.className = 'visually-hidden';
        quien.textContent = autor + ': ';
        mensaje.appendChild(quien);
        mensaje.appendChild(document.createTextNode(texto));
        mensajes.appendChild(mensaje);
        mensaje.scrollIntoView({ block: 'nearest' });
        return mensaje;
    }
})();
