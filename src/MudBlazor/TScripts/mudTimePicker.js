﻿// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

window.mudTimePicker = {
    initPointerEvents: (container, dotNetHelper) => {
        let isPointerDown = false;

        const startHandler = (event) => {
            if (event.button !== 0) {
                // Only handle main (left) pointer button.
                return;
            }

            isPointerDown = true;

            // Allow the pointerover event to trigger.
            event.target.releasePointerCapture(event.pointerId);

            // Set the selected value to the stick that the pointer went down on.
            if (event.target.classList.contains('mud-picker-stick')) {
                let attributeValue = event.target.getAttribute('data-stick-value');
                let stickValue = attributeValue ? parseInt(attributeValue) : -1; // Ensure an integer.

                dotNetHelper.invokeMethodAsync('SelectTimeFromStick', stickValue);
            }

            event.preventDefault();
        };

        const endHandler = (event) => {
            if (event.button !== 0) {
                // Only handle main (left) pointer button.
                return;
            }

            isPointerDown = false;

            event.preventDefault();
        };

        const moveHandler = (event) => {
            if (!isPointerDown || !event.target.classList.contains('mud-picker-stick')) {
                // Only update time from the stick if the pointer is down.
                return;
            }

            let attributeValue = event.target.getAttribute('data-stick-value');
            let stickValue = attributeValue ? parseInt(attributeValue) : -1; // Ensure an integer.

            dotNetHelper.invokeMethodAsync('SelectTimeFromStick', stickValue);

            event.preventDefault();
        };

        container.addEventListener('pointerdown', startHandler);
        container.addEventListener('pointerup', endHandler);
        container.addEventListener('pointercancel', endHandler);
        container.addEventListener('pointerover', moveHandler);

        container.destroy = () => {
            container.removeEventListener('pointerdown', startHandler);
            container.removeEventListener('pointerup', endHandler);
            container.removeEventListener('pointercancel', endHandler);
            container.removeEventListener('pointerover', moveHandler);
        };
    },

    destroyPointerEvents: (container) => {
        // Clean up event listeners from the picker element
        if (typeof container.destroy === 'function') {
            container.destroy();
        }
    }
};
