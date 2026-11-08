// JavaScript mínimo. Todo funciona sin JS; esto solo mejora la experiencia.
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        // Enfoca el resumen de errores del servidor para que el lector de pantalla lo lea primero.
        var resumen = document.querySelector('[data-enfocar-al-cargar]');
        if (resumen) {
            resumen.focus();
        }

        document.querySelectorAll('input[type="password"]').forEach(agregarBotonMostrarClave);

        // El botón "Menú" (pantallas chicas) anuncia si el panel está abierto o cerrado.
        document.querySelectorAll('.offcanvas-lg').forEach(function (panel) {
            var botones = document.querySelectorAll('[data-bs-target="#' + panel.id + '"][data-bs-toggle="offcanvas"]');
            botones.forEach(function (b) { b.setAttribute('aria-expanded', 'false'); });
            panel.addEventListener('shown.bs.offcanvas', function () {
                botones.forEach(function (b) { b.setAttribute('aria-expanded', 'true'); });
            });
            panel.addEventListener('hidden.bs.offcanvas', function () {
                botones.forEach(function (b) { b.setAttribute('aria-expanded', 'false'); });
            });
        });
    });

    // Botón "Mostrar / Ocultar" en todos los campos de contraseña. Se agrega con JS porque sin JS
    // no tendría función. Texto visible (no solo un ícono) para que sea claro para todos.
    function agregarBotonMostrarClave(campo) {
        var grupo = document.createElement('div');
        grupo.className = campo.classList.contains('form-control-lg') ? 'input-group input-group-lg' : 'input-group';
        campo.parentNode.insertBefore(grupo, campo);
        grupo.appendChild(campo);

        var boton = document.createElement('button');
        boton.type = 'button';
        boton.className = 'btn btn-outline-secondary boton-mostrar-clave';
        boton.setAttribute('aria-controls', campo.id);
        grupo.appendChild(boton);

        mostrarClave(campo, boton, false);
        boton.addEventListener('click', function () {
            mostrarClave(campo, boton, campo.type === 'password');
            campo.focus();
        });
    }

    function mostrarClave(campo, boton, visible) {
        campo.type = visible ? 'text' : 'password';
        boton.innerHTML = (visible ? 'Ocultar' : 'Mostrar') + '<span class="visually-hidden"> contraseña</span>';
    }

    // Antes de enviar, las contraseñas vuelven a ocultarse para que el navegador no las trate como texto común.
    function ocultarClaves(formulario) {
        formulario.querySelectorAll('.boton-mostrar-clave').forEach(function (boton) {
            var campo = document.getElementById(boton.getAttribute('aria-controls'));
            if (campo) {
                mostrarClave(campo, boton, false);
            }
        });
    }

    // Confirmación nativa en botones de acciones destructivas (además de la página de confirmación)
    // y protección contra el doble envío (doble clic, Enter + clic).
    document.addEventListener('submit', function (evento) {
        var formulario = evento.target;
        var mensaje = formulario.getAttribute('data-confirmar');
        if (mensaje && !window.confirm(mensaje)) {
            evento.preventDefault();
            return;
        }

        if (formulario.getAttribute('data-enviando') === 'true') {
            evento.preventDefault();
            return;
        }

        // Si la validación de cliente frena el envío, el formulario sigue disponible.
        if (window.jQuery && window.jQuery(formulario).data('validator') && !window.jQuery(formulario).valid()) {
            return;
        }

        ocultarClaves(formulario);
        formulario.setAttribute('data-enviando', 'true');
        formulario.setAttribute('aria-busy', 'true');
    });

    // Al volver con el botón "Atrás", el navegador puede restaurar la página con la marca puesta.
    window.addEventListener('pageshow', function () {
        document.querySelectorAll('form[data-enviando]').forEach(function (formulario) {
            formulario.removeAttribute('data-enviando');
            formulario.removeAttribute('aria-busy');
        });
    });
})();
