/** Модуль экспортирует сервис работы с задачами. */

import type { IProblem } from "../domain";

/** Сервис работы приложения. */
export class ProblemsService {
    /** Получить список текущих задач. */
    static async loadProblems(): Promise<void> {
        const { data } = await this.sendRequest( this.url );
        this.problems = ( data as string[] ).map( name => ({ name }) );
    }

    /** Получить список текущих задач. */
    static async loadProblemInfo( problemName: string ): Promise<void> {
        const { data } = await this.sendRequest( this.url, [problemName] );
        const item = this.problems.find( problem => problem.name === problemName )
    }

    /** Список задач. */
    static problems: IProblem[] = [];

    /** 
     * Метод создания и посылки запроса с дальнейшей обработкой ответа.
     * @param url - URL запроса.
     * @param [method = 'GET'] - метод запроса.
     * @param [body] - тело запроса (если необходимо).
     * */
    private static async sendRequest(
        url: string,
        pathParams: (string | number)[] = [],
        method: string = 'GET',
        bodyData?: Record<string, any>
    ): Promise<Record<string, any>> {
        const response = await fetch( url + pathParams?.map( encodeURIComponent ).join('/'), {
            method,
            headers: {
                'Content-Type': 'application/json;charset=utf-8'
            },
            ...bodyData && { body: JSON.stringify(bodyData) }
        });

        const data = await response.json();
        console.log( data )

        return data;
    }

    /** URL сервера. */
    private static readonly url = `http://${ window.location.hostname }:5277/problems`;
}
