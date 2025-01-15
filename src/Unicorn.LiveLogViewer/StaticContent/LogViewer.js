document.configuration = {
    logLevels: [
        {name: "Critical", color: '#c12ed1', icon: "💥"},
        {name: "Error", color: '#d1332e', icon: "❌"},
        {name: "Warning", color: '#d18a2e', icon: "⚠️"},
        {name: "Info", color: '#17a6e8', icon: "ℹ️️"},
        {name: "Debug", color: '#019801', icon: "🪲️"},
        {name: "Trace", color: '#808080', icon: "📃"}
    ]
};

/***********************************************************************************************************************
 * Log level filters                                                                                                    *
 ***********************************************************************************************************************/

/**
 * A component that provides filtering facilities for log levels.
 */
class LogLevelFilter extends HTMLElement {

    /**
     * Creates a new instance of the `LogLevelFilter` class.
     */
    constructor() {
        super();

        this.inputs = document.configuration.logLevels
            .map((l) => this.#renderSwitch(l.icon, l.color, l.name));
    }

    /**
     * The list of enabled log-levels
     * @returns {Array<Boolean>} The list of enabled log levels
     */
    get enabledLogLevels() {
        return this.inputs.map(i => i.checked);
    }

    /**
     *
     * @param {Array<Boolean>} value The list of enabled log levels
     */
    set enabledLogLevels(value) {
        for (let i = 0; i < value.length && i < this.inputs.length; i++) {
            this.inputs[i].checked = value[i];
        }
    }

    /**
     * Renders a single enable/disable switch.
     * @param {string} iconEmoji The emoji to use to represent the log level.
     * @param {string} color The color that represents the log level.
     * @param {string} name The name of the log level.
     * @returns {HTMLInputElement} The created input checkbox element.
     */
    #renderSwitch(iconEmoji, color, name) {
        const icon = document.createTextNode(iconEmoji);

        const input = document.createElement('input');
        input.style.color = color;
        input.role = 'switch';
        input.type = 'checkbox';
        input.checked = true;
        input.onchange = () => this.#dispatchChange();

        const label = document.createElement('label');
        label.title = name;
        label.appendChild(icon);
        label.appendChild(input);

        this.append(label);

        return input;
    }

    /**
     * Dispatches a 'change' event.
     */
    #dispatchChange() {
        // this.dispatchEvent(new Event('change'));
    }
}

// Define the custom element
customElements.define('ulv-log-level-filter', LogLevelFilter);
window.onload = function () {
    console.log('Loaded');
    const filter = document.getElementById('filter');

    console.log('Found filter component: ', filter)
    filter.addEventListener('change', () => {
        console.log(filter.enabledLogLevels);
    });
};