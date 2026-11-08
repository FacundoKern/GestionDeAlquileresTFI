// La validación de cliente de jQuery solo entiende números con punto decimal.
// Esto la adapta al formato argentino: "1.234,56" (punto de miles opcional, coma decimal).
(function ($) {
    'use strict';
    if (!$ || !$.validator) {
        return;
    }

    function aNumero(valor) {
        var limpio = String(valor).trim().replace(/\./g, '').replace(',', '.');
        return limpio === '' ? NaN : Number(limpio);
    }

    var formatoArgentino = /^-?(\d{1,3}(\.\d{3})+|\d+)(,\d+)?$/;

    $.validator.methods.number = function (valor, elemento) {
        return this.optional(elemento) || formatoArgentino.test(String(valor).trim());
    };

    $.validator.methods.range = function (valor, elemento, parametros) {
        var numero = aNumero(valor);
        return this.optional(elemento) || (numero >= Number(parametros[0]) && numero <= Number(parametros[1]));
    };

    $.validator.methods.min = function (valor, elemento, parametro) {
        return this.optional(elemento) || aNumero(valor) >= Number(parametro);
    };

    $.validator.methods.max = function (valor, elemento, parametro) {
        return this.optional(elemento) || aNumero(valor) <= Number(parametro);
    };
})(window.jQuery);
