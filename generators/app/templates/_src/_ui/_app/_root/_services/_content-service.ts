
import { default as Axios } from 'axios';
import { Store } from 'vuex';
import { GlobalConfig } from '../../common';
import { IPayload } from '../../model';
import { IRootStoreState, RootStoreTypes } from '../store';
import { StoreService } from '../../store';

const RIKER_IPSUM = GlobalConfig.uri.services + 'content/rikeripsum';

export class ContentService extends StoreService {
    constructor(store: Store<{}>) {
        super(store);
    }

    rikerIpsum() {
        let payload: IPayload<string> = null;

        return this.exec<string>(Axios.get(RIKER_IPSUM))
            .then((value) => {
                payload = value;
                return this.store.dispatch(RootStoreTypes.secureContent, value.data);
            })
            .then((value) => this.store.dispatch(RootStoreTypes.common.updateStatusBar, payload.message))
    }
}