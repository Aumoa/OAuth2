export class HttpStatusCodeError extends Error {
  #status: number;
  #statusText: string;

  constructor(status: number, statusText: string) {
    super(statusText);
    this.#status = status;
    this.#statusText = statusText;
  }

  get status() {
    return this.#status;
  }

  get statusText() {
    return this.#statusText;
  }
};