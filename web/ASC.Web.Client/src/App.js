import React, { Suspense, lazy } from 'react';
import { BrowserRouter, Route, Switch } from 'react-router-dom';
import { Loader, ErrorContainer } from 'asc-web-components';
import StudioLayout from './components/Layout/Layout';
import Login from './components/pages/Login/Login';
import Home from './components/pages/Home/Home';

import { PrivateRoute } from './helpers/privateRoute';

const About = lazy(() => import('./components/pages/About/About'));

const App = () => {
    return (
        <BrowserRouter>
            <StudioLayout>
                <Suspense fallback={<Loader className="pageLoader" type="rombs" size={40} />}>
                    <Switch>
                        <Route exact path='/login' component={Login} />
                        <PrivateRoute exact path='/' component={Home} />
                        <PrivateRoute exact path='/about' component={About} />
                        <PrivateRoute component={() => (
                            <ErrorContainer>
                                Sorry, the resource
                                cannot be found.
                            </ErrorContainer>
                        )} />
                    </Switch>
                </Suspense>
            </StudioLayout>
        </BrowserRouter>
    );
};

export default App;